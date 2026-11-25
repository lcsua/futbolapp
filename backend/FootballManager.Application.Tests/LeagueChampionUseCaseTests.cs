using FootballManager.Api.Authorization;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.AdvertisementImages;
using FootballManager.Application.UseCases.Leagues.LeagueChampions;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Tests;

public class LeagueChampionUseCaseTests
{
    [Fact]
    public async Task Historical_champion_can_be_registered_without_a_season()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Unión");

        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 1998, "Apertura", "Primera B"));

        Assert.Equal(1998, created.Year);
        Assert.Equal("Apertura", created.ChampionshipName);
        Assert.Equal("Primera B", created.DivisionName);
        Assert.Null(created.SeasonId);
        Assert.Null(created.DivisionId);
        Assert.Equal(0, created.PhotoCount);
        Assert.Empty(created.Photos);
        Assert.Equal(team.Id, created.TeamId);
    }

    [Fact]
    public async Task Historical_champion_accepts_an_archived_team()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Colegiales");
        team.SoftDelete();

        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 1974, "Anual", "Primera"));

        Assert.True(created.TeamIsArchived);
        Assert.Equal("Colegiales", created.TeamName);
    }

    [Fact]
    public async Task Existing_championship_uses_the_selected_participant_and_ignores_the_request_year()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var winner = ctx.AddTeam("Los Andes");
        var leader = ctx.AddTeam("Temperley");
        var seasonId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        ctx.Links.Add(new DivisionSeasonLink(
            Guid.NewGuid(),
            "Apertura",
            2024,
            "Primera",
            new[] { leader.Id, winner.Id }));
        ctx.LinkSeasonId = seasonId;
        ctx.LinkDivisionId = divisionId;

        var request = Historical(ctx, winner.Id, 1990, "Ignorado", "Ignorada");
        request.Mode = "existing";
        request.SeasonId = seasonId;
        request.DivisionId = divisionId;

        var created = await ctx.CreateUseCase.ExecuteAsync(request);

        Assert.Equal(winner.Id, created.TeamId);
        Assert.NotEqual(leader.Id, created.TeamId);
        Assert.Equal(2024, created.Year);
        Assert.Equal("Apertura", created.ChampionshipName);
        Assert.Equal("Primera", created.DivisionName);
        Assert.Equal(seasonId, created.SeasonId);
        Assert.Equal(divisionId, created.DivisionId);
    }

    [Fact]
    public async Task Existing_championship_rejects_a_team_that_does_not_participate()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var outsider = ctx.AddTeam("Visitante");
        var participant = ctx.AddTeam("Local");
        ctx.Links.Add(new DivisionSeasonLink(Guid.NewGuid(), "Clausura", 2025, "Primera", new[] { participant.Id }));
        ctx.LinkSeasonId = Guid.NewGuid();
        ctx.LinkDivisionId = Guid.NewGuid();

        var request = Historical(ctx, outsider.Id, 2025, "Clausura", "Primera");
        request.Mode = "existing";
        request.SeasonId = ctx.LinkSeasonId;
        request.DivisionId = ctx.LinkDivisionId;

        var ex = await Assert.ThrowsAsync<BusinessException>(() => ctx.CreateUseCase.ExecuteAsync(request));
        Assert.Contains("no participa", ex.Message);
        Assert.Empty(ctx.Champions.Items);
    }

    [Fact]
    public async Task Duplicate_titles_are_rejected_ignoring_case_and_spacing()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var first = ctx.AddTeam("Uno");
        var second = ctx.AddTeam("Dos");
        await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, first.Id, 2010, "Apertura", "Primera"));

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            ctx.CreateUseCase.ExecuteAsync(Historical(ctx, second.Id, 2010, "  apertura ", "primera")));

        Assert.Contains("Ya existe", ex.Message);
        Assert.Single(ctx.Champions.Items);
    }

    [Fact]
    public async Task Same_title_can_exist_in_another_league()
    {
        var first = ChampionTestContext.Create();
        first.GrantAccess();
        var second = ChampionTestContext.Create();
        second.GrantAccess();
        second.Champions = first.Champions;
        second.Teams = first.Teams;
        first.Rebind();
        second.Rebind();

        var teamA = first.AddTeam("A");
        var teamB = second.AddTeam("B");
        await first.CreateUseCase.ExecuteAsync(Historical(first, teamA.Id, 2001, "Anual", "Única"));
        var created = await second.CreateUseCase.ExecuteAsync(Historical(second, teamB.Id, 2001, "Anual", "Única"));

        Assert.Equal(2, first.Champions.Items.Count);
        Assert.Equal(second.League.Id, first.Champions.Items.Single(c => c.Id == created.Id).LeagueId);
    }

    [Fact]
    public async Task Historical_record_can_later_be_linked_without_duplicating_the_title()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Defensores");
        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 1988, "Clausura", "Segunda"));

        var seasonId = Guid.NewGuid();
        var divisionId = Guid.NewGuid();
        ctx.LinkSeasonId = seasonId;
        ctx.LinkDivisionId = divisionId;
        ctx.Links.Add(new DivisionSeasonLink(Guid.NewGuid(), "Clausura 1988", 1988, "Segunda", Array.Empty<Guid>()));

        var update = Historical(ctx, team.Id, 1988, "Clausura", "Segunda");
        update.SeasonId = seasonId;
        update.DivisionId = divisionId;
        update.WonAt = new DateOnly(1988, 12, 10);
        update.Notes = "Campaña invicta";

        var updated = await ctx.Update.ExecuteAsync(created.Id, update);

        Assert.Equal(created.Id, updated.Id);
        Assert.Single(ctx.Champions.Items, c => c.DeletedAt == null);
        Assert.Equal(seasonId, updated.SeasonId);
        Assert.Equal(divisionId, updated.DivisionId);
        Assert.Equal(new DateOnly(1988, 12, 10), updated.WonAt);
        Assert.Equal("Campaña invicta", updated.Notes);
    }

    [Fact]
    public async Task List_filters_and_orders_by_year_descending()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Centro");
        await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2000, "Apertura", "Primera"));
        await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2012, "Clausura", "Primera"));
        await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2012, "Apertura", "Reserva"));

        var filtered = await ctx.List.ExecuteAsync(ctx.League.Id, ctx.UserId, new LeagueChampionListFilter
        {
            Year = 2012,
            Division = "prim",
        });

        Assert.Single(filtered);
        Assert.Equal("Clausura", filtered[0].ChampionshipName);

        var ordered = await ctx.List.ExecuteAsync(ctx.League.Id, ctx.UserId, new LeagueChampionListFilter());
        Assert.Equal(new[] { 2012, 2012, 2000 }, ordered.Select(c => c.Year).ToArray());
    }

    [Fact]
    public async Task Update_and_delete_are_isolated_between_leagues()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Local");
        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 1999, "Anual", "Primera"));

        var other = ChampionTestContext.Create();
        other.GrantAccess();
        other.Champions.Items.Add(ctx.Champions.Items.Single());

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.GetOne.ExecuteAsync(other.League.Id, created.Id, other.UserId));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.Update.ExecuteAsync(created.Id, Historical(other, team.Id, 1999, "Anual", "Primera")));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => other.Delete.ExecuteAsync(other.League.Id, created.Id, other.UserId));

        var original = await ctx.GetOne.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId);
        Assert.Equal("Anual", original.ChampionshipName);
        Assert.Null(ctx.Champions.Items.Single().DeletedAt);
    }

    [Fact]
    public async Task Delete_soft_deletes_the_champion_and_returns_photo_urls()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Norte");
        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2005, "Apertura", "Primera"));
        await ctx.AddPhotos.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId, new[]
        {
            "https://host/uploads/leagues/x/champions/y/a.jpg"
        });

        var urls = await ctx.Delete.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId);

        Assert.Equal(new[] { "https://host/uploads/leagues/x/champions/y/a.jpg" }, urls);
        Assert.Null(await ctx.Champions.GetByIdAsync(created.Id));
        Assert.NotNull(ctx.Champions.Items.Single().DeletedAt);
        Assert.Empty(await ctx.List.ExecuteAsync(ctx.League.Id, ctx.UserId, new LeagueChampionListFilter()));
    }

    [Fact]
    public async Task Photos_can_be_added_later_reordered_recovered_and_removed()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var team = ctx.AddTeam("Sur");
        var created = await ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2018, "Clausura", "Primera"));
        Assert.Equal(0, created.PhotoCount);

        var withPhotos = await ctx.AddPhotos.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId, new[]
        {
            "https://host/uploads/leagues/x/champions/y/one.jpg",
            "https://host/uploads/leagues/x/champions/y/two.png",
        });

        Assert.Equal(2, withPhotos.Photos.Count);
        Assert.True(withPhotos.Photos[0].IsCover);
        Assert.False(withPhotos.Photos[1].IsCover);
        Assert.EndsWith("one.thumb.webp", withPhotos.Photos[0].ThumbUrl);

        var cover = withPhotos.Photos[1];
        var other = withPhotos.Photos[0];
        var reordered = await ctx.Gallery.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId, new[]
        {
            new LeagueChampionPhotoWrite { Id = cover.Id, Caption = "Final", DisplayOrder = 0, IsCover = true },
            new LeagueChampionPhotoWrite { Id = other.Id, Caption = "Plantel", DisplayOrder = 1, IsCover = false },
        });

        Assert.Equal(cover.Id, reordered.Photos.Single(p => p.IsCover).Id);
        Assert.Equal("Final", reordered.Photos[0].Caption);
        Assert.Equal(other.Id, reordered.Photos[1].Id);

        await Assert.ThrowsAsync<BusinessException>(() => ctx.Gallery.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId, new[]
        {
            new LeagueChampionPhotoWrite { Id = cover.Id, DisplayOrder = 0, IsCover = true },
            new LeagueChampionPhotoWrite { Id = other.Id, DisplayOrder = 1, IsCover = true },
        }));

        await ctx.DeletePhoto.ExecuteAsync(ctx.League.Id, created.Id, cover.Id, ctx.UserId);
        var remaining = await ctx.GetOne.ExecuteAsync(ctx.League.Id, created.Id, ctx.UserId);
        Assert.Single(remaining.Photos);
        Assert.False(remaining.Photos[0].IsCover);
    }

    [Fact]
    public void Invalid_image_files_use_the_existing_upload_rules()
    {
        Assert.NotNull(AdvertisementImageRules.Validate("foto.gif", "image/gif", 1000));
        Assert.NotNull(AdvertisementImageRules.Validate("foto.jpg", "image/jpeg", AdvertisementImageRules.MaxFileBytes + 1));
        Assert.Null(AdvertisementImageRules.Validate("foto.webp", "image/webp", 1024));
    }

    [Fact]
    public void Champions_routes_require_the_leagues_permission()
    {
        var requirement = LeagueRoutePermissionResolver.Resolve("POST", "/api/leagues/11111111-1111-1111-1111-111111111111/champions");
        Assert.NotNull(requirement);
        Assert.Contains(PermissionCodes.Leagues, requirement!.AnyOf);

        var read = LeagueRoutePermissionResolver.Resolve("GET", "/api/leagues/11111111-1111-1111-1111-111111111111/champions/options");
        Assert.Contains(PermissionCodes.Leagues, read!.AnyOf);
    }

    [Fact]
    public void Inactive_teams_include_archived_and_teams_outside_the_active_season()
    {
        var activeSeason = Guid.NewGuid();
        var closedSeason = Guid.NewGuid();
        var playing = Guid.NewGuid();
        var resting = Guid.NewGuid();
        var archived = Guid.NewGuid();

        var teams = LeagueChampionCatalogMapper.WithActivity(
            new[]
            {
                new LeagueChampionTeamOptionDto(playing, "Activo", null, false, false),
                new LeagueChampionTeamOptionDto(resting, "Inactivo", null, false, false),
                new LeagueChampionTeamOptionDto(archived, "Archivado", null, true, false),
            },
            new[]
            {
                new LeagueChampionSeasonOptionDto(activeSeason, "Apertura", 2026, true),
                new LeagueChampionSeasonOptionDto(closedSeason, "Clausura", 2025, false),
            },
            new[]
            {
                new LeagueChampionRosterDto(activeSeason, Guid.NewGuid(), new[] { playing, archived }),
                new LeagueChampionRosterDto(closedSeason, Guid.NewGuid(), new[] { resting }),
            });

        Assert.False(teams.Single(t => t.Id == playing).IsInactive);
        Assert.True(teams.Single(t => t.Id == resting).IsInactive);
        Assert.True(teams.Single(t => t.Id == archived).IsInactive);
    }

    [Fact]
    public async Task Team_from_another_league_is_rejected()
    {
        var ctx = ChampionTestContext.Create();
        ctx.GrantAccess();
        var foreign = ChampionTestContext.Create();
        var team = foreign.AddTeam("Ajeno");
        ctx.Teams.Items.Add(team);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            ctx.CreateUseCase.ExecuteAsync(Historical(ctx, team.Id, 2002, "Anual", "Primera")));
    }

    private static LeagueChampionWriteRequest Historical(ChampionTestContext ctx, Guid teamId, int year, string championship, string division)
        => new()
        {
            LeagueId = ctx.League.Id,
            UserId = ctx.UserId,
            Mode = "historical",
            TeamId = teamId,
            Year = year,
            ChampionshipName = championship,
            DivisionName = division,
        };

    private sealed class ChampionTestContext
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public League League { get; }
        public FakeLeagueRepository Leagues { get; }
        public FakeTeamRepository Teams { get; set; } = new();
        public FakeChampionRepository Champions { get; set; } = new();
        public FakeUserLeagueRepository UserLeagues { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public List<DivisionSeasonLink> Links { get; } = new();
        public Guid LinkSeasonId { get; set; }
        public Guid LinkDivisionId { get; set; }
        public CreateLeagueChampionUseCase CreateUseCase { get; private set; } = null!;
        public UpdateLeagueChampionUseCase Update { get; private set; } = null!;
        public DeleteLeagueChampionUseCase Delete { get; private set; } = null!;
        public GetLeagueChampionsUseCase List { get; private set; } = null!;
        public GetLeagueChampionUseCase GetOne { get; private set; } = null!;
        public AddLeagueChampionPhotosUseCase AddPhotos { get; private set; } = null!;
        public UpdateLeagueChampionGalleryUseCase Gallery { get; private set; } = null!;
        public DeleteLeagueChampionPhotoUseCase DeletePhoto { get; private set; } = null!;

        private ChampionTestContext()
        {
            League = new League("Liga Test", "AR", $"liga-{Guid.NewGuid():N}");
            Leagues = new FakeLeagueRepository(League);
            Champions.Owner = this;
            Rebind();
        }

        public void Rebind()
        {
            CreateUseCase = new CreateLeagueChampionUseCase(Leagues, Teams, Champions, UserLeagues, UnitOfWork);
            Update = new UpdateLeagueChampionUseCase(Teams, Champions, UserLeagues, UnitOfWork);
            Delete = new DeleteLeagueChampionUseCase(Champions, UserLeagues, UnitOfWork);
            List = new GetLeagueChampionsUseCase(Champions, UserLeagues);
            GetOne = new GetLeagueChampionUseCase(Champions, UserLeagues);
            AddPhotos = new AddLeagueChampionPhotosUseCase(Champions, UserLeagues, UnitOfWork);
            Gallery = new UpdateLeagueChampionGalleryUseCase(Champions, UserLeagues, UnitOfWork);
            DeletePhoto = new DeleteLeagueChampionPhotoUseCase(Champions, UserLeagues, UnitOfWork);
        }

        public static ChampionTestContext Create() => new();

        public void GrantAccess() => UserLeagues.Grant(UserId, League.Id);

        public Team AddTeam(string name)
        {
            var team = new Team(League, name, $"{name.ToLowerInvariant()}-{Guid.NewGuid():N}"[..30]);
            Teams.Items.Add(team);
            return team;
        }
    }

    private sealed class FakeChampionRepository : ILeagueChampionRepository
    {
        public ChampionTestContext Owner { get; set; } = null!;
        public List<LeagueChampion> Items { get; } = new();

        public Task<LeagueChampion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(c => c.Id == id && c.DeletedAt == null));

        public Task<List<LeagueChampion>> ListAsync(Guid leagueId, LeagueChampionListFilter filter, CancellationToken cancellationToken = default)
        {
            IEnumerable<LeagueChampion> query = Items.Where(c => c.LeagueId == leagueId && c.DeletedAt == null);
            if (filter.Year.HasValue)
                query = query.Where(c => c.Year == filter.Year.Value);
            if (filter.TeamId.HasValue)
                query = query.Where(c => c.TeamId == filter.TeamId.Value);
            if (!string.IsNullOrWhiteSpace(filter.Championship))
            {
                var term = filter.Championship.Trim();
                query = query.Where(c => c.ChampionshipName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(filter.Division))
            {
                var term = filter.Division.Trim();
                query = query.Where(c => c.DivisionName.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult(query
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.WonAt)
                .ThenBy(c => c.ChampionshipName)
                .ThenBy(c => c.DivisionName)
                .ToList());
        }

        public Task<bool> ExistsActiveIdentityAsync(Guid leagueId, string identityKey, Guid? excludeId, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(c =>
                c.LeagueId == leagueId
                && c.DeletedAt == null
                && c.IdentityKey == identityKey
                && (!excludeId.HasValue || c.Id != excludeId.Value)));

        public Task<bool> TeamHasActiveTitleAsync(Guid teamId, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(c => c.TeamId == teamId && c.DeletedAt == null));

        public Task<DivisionSeasonLink?> GetDivisionSeasonLinkAsync(Guid leagueId, Guid seasonId, Guid divisionId, CancellationToken cancellationToken = default)
        {
            if (Owner.League.Id != leagueId || seasonId != Owner.LinkSeasonId || divisionId != Owner.LinkDivisionId)
                return Task.FromResult<DivisionSeasonLink?>(null);
            return Task.FromResult(Owner.Links.FirstOrDefault());
        }

        public Task<bool> DivisionBelongsToLeagueAsync(Guid leagueId, Guid divisionId, CancellationToken cancellationToken = default)
            => Task.FromResult(Owner.League.Id == leagueId && divisionId == Owner.LinkDivisionId);

        public Task<LeagueChampionCatalog> GetCatalogAsync(Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task AddAsync(LeagueChampion champion, CancellationToken cancellationToken = default)
        {
            Items.Add(champion);
            return Task.CompletedTask;
        }

        public Task UnlinkDivisionAsync(Guid divisionId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeTeamRepository : ITeamRepository
    {
        public List<Team> Items { get; } = new();

        public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(t => t.Id == id));

        public Task<Team?> GetByLeagueIdAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<Team>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<List<Team>> GetNeverAssignedByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<bool> HasAnySeasonAssignmentAsync(Guid teamId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task AddAsync(Team team, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public void Update(Team team) => throw new NotImplementedException();

        public Task RemoveAsync(Team team, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
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
        private readonly HashSet<(Guid UserId, Guid LeagueId)> _memberships = new();

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
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
