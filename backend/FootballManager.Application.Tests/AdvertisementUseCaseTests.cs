using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.CreateAdvertisement;
using FootballManager.Application.UseCases.Leagues.DeleteAdvertisement;
using FootballManager.Application.UseCases.Leagues.GetAdvertisement;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Application.UseCases.Leagues.RemoveAdvertisementImage;
using FootballManager.Application.UseCases.Leagues.SetAdvertisementImage;
using FootballManager.Application.UseCases.Leagues.UpdateAdvertisement;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Tests;

public class AdvertisementUseCaseTests
{
    [Fact]
    public void Domain_accepts_a_valid_advertisement()
    {
        var league = NewLeague();
        var starts = DateTime.UtcNow;
        var ends = starts.AddDays(7);

        var ad = new Advertisement(
            league,
            "Banner principal",
            "Sponsor SA",
            AdvertisementSlot.LeagueTop,
            "https://cdn.example/desktop.png",
            null,
            "https://sponsor.example",
            starts,
            ends,
            0,
            true);

        Assert.Equal(league.Id, ad.LeagueId);
        Assert.Equal("Banner principal", ad.Name);
        Assert.Equal("Sponsor SA", ad.AdvertiserName);
        Assert.Equal(AdvertisementSlot.LeagueTop, ad.Slot);
        Assert.Equal(0, ad.Priority);
        Assert.True(ad.IsActive);
    }

    [Fact]
    public void Domain_rejects_invalid_schedule()
    {
        var league = NewLeague();
        var starts = DateTime.UtcNow;
        var ends = starts.AddDays(-1);

        var ex = Assert.Throws<ArgumentException>(() => new Advertisement(
            league,
            "Banner",
            "Sponsor",
            AdvertisementSlot.LeagueMiddle,
            startsAt: starts,
            endsAt: ends));

        Assert.Contains("EndsAt", ex.Message);
    }

    [Fact]
    public async Task Create_persists_a_valid_advertisement()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();

        var result = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        var stored = await ctx.Advertisements.GetByIdAsync(result.Id);
        Assert.NotNull(stored);
        Assert.Equal(ctx.League.Id, stored!.LeagueId);
        Assert.Equal("Banner principal", stored.Name);
        Assert.Equal(AdvertisementSlot.LeagueTop, stored.Slot);
        Assert.True(stored.IsActive);
        Assert.Equal(1, ctx.UnitOfWork.SaveCount);
    }

    [Fact]
    public async Task Create_rejects_invalid_schedule()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();

        var request = ValidCreate(ctx);
        request.StartsAt = DateTime.UtcNow;
        request.EndsAt = request.StartsAt.Value.AddHours(-2);

        await Assert.ThrowsAsync<ArgumentException>(() => ctx.CreateUseCase.ExecuteAsync(request));
        Assert.Empty(ctx.Advertisements.Items);
    }

    [Fact]
    public async Task Update_changes_advertisement_details()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        await ctx.Update.ExecuteAsync(new UpdateAdvertisementRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Name = "Banner actualizado",
            AdvertiserName = "Otro sponsor",
            Slot = AdvertisementSlot.ResultsFixture,
            Priority = 5,
            IsActive = false,
        });

        var stored = await ctx.Advertisements.GetByIdAsync(created.Id);
        Assert.Equal("Banner actualizado", stored!.Name);
        Assert.Equal("Otro sponsor", stored.AdvertiserName);
        Assert.Equal(AdvertisementSlot.ResultsFixture, stored.Slot);
        Assert.Equal(5, stored.Priority);
        Assert.False(stored.IsActive);
    }

    [Fact]
    public async Task Get_update_and_delete_reject_advertisement_from_another_league()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        var other = AdvertisementTestContext.Create();
        other.GrantAccess();
        var existing = await ctx.Advertisements.GetByIdAsync(created.Id);
        other.Advertisements.Items.Add(existing!);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.GetOne.ExecuteAsync(new GetAdvertisementRequest
        {
            LeagueId = other.League.Id,
            AdvertisementId = created.Id,
            UserId = other.UserId,
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.Update.ExecuteAsync(new UpdateAdvertisementRequest
        {
            LeagueId = other.League.Id,
            AdvertisementId = created.Id,
            UserId = other.UserId,
            Name = "Hack",
            AdvertiserName = "Hack",
            Slot = AdvertisementSlot.LeagueTop,
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.Delete.ExecuteAsync(new DeleteAdvertisementRequest
        {
            LeagueId = other.League.Id,
            AdvertisementId = created.Id,
            UserId = other.UserId,
        }));

        var original = await ctx.Advertisements.GetByIdAsync(created.Id);
        Assert.Equal("Banner principal", original!.Name);
        Assert.Null(original.DeletedAt);
    }

    [Fact]
    public async Task Delete_soft_deletes_the_advertisement()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        await ctx.Delete.ExecuteAsync(new DeleteAdvertisementRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
        });

        Assert.Null(await ctx.Advertisements.GetByIdAsync(created.Id));
        Assert.NotNull(ctx.Advertisements.Items.Single(a => a.Id == created.Id).DeletedAt);
        Assert.Empty(await ctx.Advertisements.GetByLeagueIdAsync(ctx.League.Id));
    }

    [Fact]
    public async Task List_returns_only_advertisements_of_the_requested_league()
    {
        var leagueA = AdvertisementTestContext.Create();
        leagueA.GrantAccess();
        var leagueB = AdvertisementTestContext.Create();
        leagueB.Advertisements = leagueA.Advertisements;
        leagueB.GrantAccess();

        var sharedCreate = new CreateAdvertisementUseCase(
            leagueA.Leagues,
            leagueA.Advertisements,
            leagueA.UserLeagues,
            leagueA.UnitOfWork);
        await sharedCreate.ExecuteAsync(ValidCreate(leagueA, "A1"));

        var createB = new CreateAdvertisementUseCase(
            leagueB.Leagues,
            leagueB.Advertisements,
            leagueB.UserLeagues,
            leagueB.UnitOfWork);
        await createB.ExecuteAsync(ValidCreate(leagueB, "B1"));

        var listA = await new GetAdvertisementsUseCase(leagueA.Advertisements, leagueA.UserLeagues)
            .ExecuteAsync(new GetAdvertisementsRequest { LeagueId = leagueA.League.Id, UserId = leagueA.UserId });
        var listB = await new GetAdvertisementsUseCase(leagueB.Advertisements, leagueB.UserLeagues)
            .ExecuteAsync(new GetAdvertisementsRequest { LeagueId = leagueB.League.Id, UserId = leagueB.UserId });

        Assert.Single(listA.Advertisements);
        Assert.Equal("A1", listA.Advertisements[0].Name);
        Assert.Equal(leagueA.League.Id, listA.Advertisements[0].LeagueId);

        Assert.Single(listB.Advertisements);
        Assert.Equal("B1", listB.Advertisements[0].Name);
        Assert.Equal(leagueB.League.Id, listB.Advertisements[0].LeagueId);
    }

    [Fact]
    public async Task Set_desktop_image_stores_the_url()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        var result = await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/leagues/x/advertisements/y/desktop-1.png",
        });

        Assert.Equal("https://host/uploads/leagues/x/advertisements/y/desktop-1.png", result.Advertisement.DesktopImageUrl);
        Assert.Null(result.PreviousImageUrl);
        Assert.Null(result.Advertisement.MobileImageUrl);
    }

    [Fact]
    public async Task Set_mobile_image_stores_the_url()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        var result = await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Mobile,
            ImageUrl = "https://host/uploads/leagues/x/advertisements/y/mobile-1.webp",
        });

        Assert.Equal("https://host/uploads/leagues/x/advertisements/y/mobile-1.webp", result.Advertisement.MobileImageUrl);
        Assert.Null(result.Advertisement.DesktopImageUrl);
    }

    [Fact]
    public async Task Set_image_returns_previous_url_on_replace()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/old.png",
        });

        var replaced = await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/new.png",
        });

        Assert.Equal("https://host/uploads/old.png", replaced.PreviousImageUrl);
        Assert.Equal("https://host/uploads/new.png", replaced.Advertisement.DesktopImageUrl);
    }

    [Fact]
    public async Task Remove_image_clears_the_url()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));
        await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Mobile,
            ImageUrl = "https://host/uploads/mobile.png",
        });

        var removed = await ctx.RemoveImage.ExecuteAsync(new RemoveAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Mobile,
        });

        Assert.Equal("https://host/uploads/mobile.png", removed.PreviousImageUrl);
        Assert.Null(removed.Advertisement.MobileImageUrl);
        Assert.NotNull(await ctx.Advertisements.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task Update_does_not_change_managed_images()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));
        await ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/keep.png",
        });

        await ctx.Update.ExecuteAsync(new UpdateAdvertisementRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = created.Id,
            UserId = ctx.UserId,
            Name = "Renamed",
            AdvertiserName = "Sponsor SA",
            Slot = AdvertisementSlot.LeagueTop,
        });

        var stored = await ctx.Advertisements.GetByIdAsync(created.Id);
        Assert.Equal("Renamed", stored!.Name);
        Assert.Equal("https://host/uploads/keep.png", stored.DesktopImageUrl);
    }

    [Fact]
    public async Task Image_operations_reject_missing_or_foreign_advertisements()
    {
        var ctx = AdvertisementTestContext.Create();
        ctx.GrantAccess();
        var created = await ctx.CreateUseCase.ExecuteAsync(ValidCreate(ctx));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => ctx.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = ctx.League.Id,
            AdvertisementId = Guid.NewGuid(),
            UserId = ctx.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/x.png",
        }));

        var other = AdvertisementTestContext.Create();
        other.GrantAccess();
        other.Advertisements.Items.Add((await ctx.Advertisements.GetByIdAsync(created.Id))!);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.SetImage.ExecuteAsync(new SetAdvertisementImageRequest
        {
            LeagueId = other.League.Id,
            AdvertisementId = created.Id,
            UserId = other.UserId,
            Kind = AdvertisementImageKind.Desktop,
            ImageUrl = "https://host/uploads/hack.png",
        }));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.RemoveImage.ExecuteAsync(new RemoveAdvertisementImageRequest
        {
            LeagueId = other.League.Id,
            AdvertisementId = created.Id,
            UserId = other.UserId,
            Kind = AdvertisementImageKind.Desktop,
        }));
    }

    private static League NewLeague(string slug = "liga-test")
        => new("Liga Test", "AR", slug);

    private static CreateAdvertisementRequest ValidCreate(AdvertisementTestContext ctx, string name = "Banner principal")
        => new()
        {
            LeagueId = ctx.League.Id,
            UserId = ctx.UserId,
            Name = name,
            AdvertiserName = "Sponsor SA",
            Slot = AdvertisementSlot.LeagueTop,
            Priority = 0,
            IsActive = true,
        };

    private sealed class AdvertisementTestContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public League League { get; } = NewLeague($"liga-{Guid.NewGuid():N}");
        public FakeLeagueRepository Leagues { get; }
        public FakeAdvertisementRepository Advertisements { get; set; } = new();
        public FakeUserLeagueRepository UserLeagues { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public CreateAdvertisementUseCase CreateUseCase { get; }
        public UpdateAdvertisementUseCase Update { get; }
        public DeleteAdvertisementUseCase Delete { get; }
        public GetAdvertisementUseCase GetOne { get; }
        public GetAdvertisementsUseCase GetAll { get; }
        public SetAdvertisementImageUseCase SetImage { get; }
        public RemoveAdvertisementImageUseCase RemoveImage { get; }

        private AdvertisementTestContext()
        {
            Leagues = new FakeLeagueRepository(League);
            CreateUseCase = new CreateAdvertisementUseCase(Leagues, Advertisements, UserLeagues, UnitOfWork);
            Update = new UpdateAdvertisementUseCase(Advertisements, UserLeagues, UnitOfWork);
            Delete = new DeleteAdvertisementUseCase(Advertisements, UserLeagues, UnitOfWork);
            GetOne = new GetAdvertisementUseCase(Advertisements, UserLeagues);
            GetAll = new GetAdvertisementsUseCase(Advertisements, UserLeagues);
            SetImage = new SetAdvertisementImageUseCase(Advertisements, UserLeagues, UnitOfWork);
            RemoveImage = new RemoveAdvertisementImageUseCase(Advertisements, UserLeagues, UnitOfWork);
        }

        public static AdvertisementTestContext Create() => new();

        public void GrantAccess() => UserLeagues.Grant(UserId, League.Id);
    }

    private sealed class FakeAdvertisementRepository : IAdvertisementRepository
    {
        public List<Advertisement> Items { get; } = [];

        public Task<Advertisement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(a => a.Id == id && a.DeletedAt == null));

        public Task<List<Advertisement>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
            => Task.FromResult(Items
                .Where(a => a.LeagueId == leagueId && a.DeletedAt == null)
                .OrderBy(a => a.Slot)
                .ThenByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToList());

        public Task AddAsync(Advertisement advertisement, CancellationToken cancellationToken = default)
        {
            Items.Add(advertisement);
            return Task.CompletedTask;
        }

        public void Update(Advertisement advertisement)
        {
            var index = Items.FindIndex(a => a.Id == advertisement.Id);
            if (index >= 0)
                Items[index] = advertisement;
        }
    }

    private sealed class FakeLeagueRepository : ILeagueRepository
    {
        private readonly League _league;

        public FakeLeagueRepository(League league) => _league = league;

        public Task<League?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == _league.Id ? _league : null);

        public Task<League?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<League>> GetAllAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<League>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> ExistsBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task AddAsync(League league, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public void Update(League league) => throw new NotImplementedException();

        public void Delete(League league) => throw new NotImplementedException();
    }

    private sealed class FakeUserLeagueRepository : IUserLeagueRepository
    {
        private readonly HashSet<(Guid UserId, Guid LeagueId)> _memberships = [];

        public void Grant(Guid userId, Guid leagueId) => _memberships.Add((userId, leagueId));

        public Task<bool> IsUserInLeagueAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
            => Task.FromResult(_memberships.Contains((userId, leagueId)));

        public Task AddAsync(UserLeague userLeague, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<UserLeague?> GetAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<UserLeague?> GetWithRoleAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<UserLeague>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> HasPermissionInAnyLeagueAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<int> CountByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public void Remove(UserLeague userLeague) => throw new NotImplementedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
