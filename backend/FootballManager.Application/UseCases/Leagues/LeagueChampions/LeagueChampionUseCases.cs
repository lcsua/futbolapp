using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.LeagueChampions
{
    public interface IGetLeagueChampionsUseCase
    {
        Task<IReadOnlyList<LeagueChampionListItemDto>> ExecuteAsync(
            Guid leagueId,
            Guid userId,
            LeagueChampionListFilter filter,
            CancellationToken cancellationToken = default);
    }

    public interface IGetLeagueChampionUseCase
    {
        Task<LeagueChampionDetailDto> ExecuteAsync(Guid leagueId, Guid championId, Guid userId, CancellationToken cancellationToken = default);
    }

    public interface IGetLeagueChampionOptionsUseCase
    {
        Task<LeagueChampionOptionsResponse> ExecuteAsync(Guid leagueId, Guid userId, CancellationToken cancellationToken = default);
    }

    public sealed class LeagueChampionOptionsResponse
    {
        public IReadOnlyList<LeagueChampionSeasonOptionDto> Seasons { get; init; } = Array.Empty<LeagueChampionSeasonOptionDto>();
        public IReadOnlyList<LeagueChampionDivisionOptionDto> Divisions { get; init; } = Array.Empty<LeagueChampionDivisionOptionDto>();
        public IReadOnlyList<LeagueChampionTeamOptionDto> Teams { get; init; } = Array.Empty<LeagueChampionTeamOptionDto>();
        public IReadOnlyList<LeagueChampionRosterDto> Participants { get; init; } = Array.Empty<LeagueChampionRosterDto>();
    }

    public interface ICreateLeagueChampionUseCase
    {
        Task<LeagueChampionDetailDto> ExecuteAsync(LeagueChampionWriteRequest request, CancellationToken cancellationToken = default);
    }

    public interface IUpdateLeagueChampionUseCase
    {
        Task<LeagueChampionDetailDto> ExecuteAsync(Guid championId, LeagueChampionWriteRequest request, CancellationToken cancellationToken = default);
    }

    public interface IDeleteLeagueChampionUseCase
    {
        Task<IReadOnlyList<string>> ExecuteAsync(Guid leagueId, Guid championId, Guid userId, CancellationToken cancellationToken = default);
    }

    public interface IAddLeagueChampionPhotosUseCase
    {
        Task<LeagueChampionDetailDto> ExecuteAsync(
            Guid leagueId,
            Guid championId,
            Guid userId,
            IReadOnlyList<string> imageUrls,
            CancellationToken cancellationToken = default);
    }

    public interface IUpdateLeagueChampionGalleryUseCase
    {
        Task<LeagueChampionDetailDto> ExecuteAsync(
            Guid leagueId,
            Guid championId,
            Guid userId,
            IReadOnlyList<LeagueChampionPhotoWrite> photos,
            CancellationToken cancellationToken = default);
    }

    public interface IDeleteLeagueChampionPhotoUseCase
    {
        Task<string> ExecuteAsync(Guid leagueId, Guid championId, Guid photoId, Guid userId, CancellationToken cancellationToken = default);
    }

    public sealed class GetLeagueChampionsUseCase : IGetLeagueChampionsUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;

        public GetLeagueChampionsUseCase(ILeagueChampionRepository champions, IUserLeagueRepository userLeagues)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
        }

        public async Task<IReadOnlyList<LeagueChampionListItemDto>> ExecuteAsync(
            Guid leagueId,
            Guid userId,
            LeagueChampionListFilter filter,
            CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var rows = await _champions.ListAsync(leagueId, filter ?? new LeagueChampionListFilter(), cancellationToken);
            return rows.Select(LeagueChampionMapper.ToListItem).ToList();
        }
    }

    public sealed class GetLeagueChampionUseCase : IGetLeagueChampionUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;

        public GetLeagueChampionUseCase(ILeagueChampionRepository champions, IUserLeagueRepository userLeagues)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
        }

        public async Task<LeagueChampionDetailDto> ExecuteAsync(Guid leagueId, Guid championId, Guid userId, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, leagueId, championId, cancellationToken);
            return LeagueChampionMapper.ToDetail(champion);
        }
    }

    public sealed class GetLeagueChampionOptionsUseCase : IGetLeagueChampionOptionsUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;

        public GetLeagueChampionOptionsUseCase(ILeagueChampionRepository champions, IUserLeagueRepository userLeagues)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
        }

        public async Task<LeagueChampionOptionsResponse> ExecuteAsync(Guid leagueId, Guid userId, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var catalog = await _champions.GetCatalogAsync(leagueId, cancellationToken);
            return new LeagueChampionOptionsResponse
            {
                Seasons = catalog.Seasons,
                Divisions = catalog.Divisions,
                Teams = LeagueChampionCatalogMapper.WithActivity(catalog.Teams, catalog.Seasons, catalog.Rosters),
                Participants = catalog.Rosters,
            };
        }
    }

    public sealed class CreateLeagueChampionUseCase : ICreateLeagueChampionUseCase
    {
        private readonly ILeagueRepository _leagues;
        private readonly ITeamRepository _teams;
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public CreateLeagueChampionUseCase(
            ILeagueRepository leagues,
            ITeamRepository teams,
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _leagues = leagues ?? throw new ArgumentNullException(nameof(leagues));
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<LeagueChampionDetailDto> ExecuteAsync(LeagueChampionWriteRequest request, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, request.UserId, request.LeagueId, cancellationToken);

            var league = await _leagues.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException("No se encontró la liga.");

            var resolved = await LeagueChampionEditor.ResolveAsync(_champions, _teams, request.LeagueId, request, cancellationToken);
            await LeagueChampionEditor.EnsureUniqueAsync(
                _champions,
                request.LeagueId,
                resolved.Year,
                resolved.ChampionshipName,
                resolved.DivisionName,
                excludeId: null,
                cancellationToken);

            LeagueChampion champion;
            try
            {
                champion = new LeagueChampion(
                    league,
                    resolved.Team,
                    resolved.Year,
                    resolved.ChampionshipName,
                    resolved.DivisionName,
                    resolved.SeasonId,
                    resolved.DivisionId,
                    resolved.WonAt,
                    resolved.Notes);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            await _champions.AddAsync(champion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return LeagueChampionMapper.ToDetail(champion);
        }
    }

    public sealed class UpdateLeagueChampionUseCase : IUpdateLeagueChampionUseCase
    {
        private readonly ITeamRepository _teams;
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLeagueChampionUseCase(
            ITeamRepository teams,
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _teams = teams ?? throw new ArgumentNullException(nameof(teams));
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<LeagueChampionDetailDto> ExecuteAsync(Guid championId, LeagueChampionWriteRequest request, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, request.UserId, request.LeagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, request.LeagueId, championId, cancellationToken);
            var resolved = await LeagueChampionEditor.ResolveAsync(_champions, _teams, request.LeagueId, request, cancellationToken);
            await LeagueChampionEditor.EnsureUniqueAsync(
                _champions,
                request.LeagueId,
                resolved.Year,
                resolved.ChampionshipName,
                resolved.DivisionName,
                champion.Id,
                cancellationToken);

            try
            {
                champion.Update(
                    resolved.Team,
                    resolved.Year,
                    resolved.ChampionshipName,
                    resolved.DivisionName,
                    resolved.SeasonId,
                    resolved.DivisionId,
                    resolved.WonAt,
                    resolved.Notes);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return LeagueChampionMapper.ToDetail(champion);
        }
    }

    public sealed class DeleteLeagueChampionUseCase : IDeleteLeagueChampionUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLeagueChampionUseCase(
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<IReadOnlyList<string>> ExecuteAsync(Guid leagueId, Guid championId, Guid userId, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, leagueId, championId, cancellationToken);
            var urls = champion.SoftDeleteWithPhotos();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return urls;
        }
    }

    public sealed class AddLeagueChampionPhotosUseCase : IAddLeagueChampionPhotosUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public AddLeagueChampionPhotosUseCase(
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<LeagueChampionDetailDto> ExecuteAsync(
            Guid leagueId,
            Guid championId,
            Guid userId,
            IReadOnlyList<string> imageUrls,
            CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, leagueId, championId, cancellationToken);

            try
            {
                champion.AddPhotos(imageUrls);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("at most", StringComparison.Ordinal))
            {
                throw new BusinessException($"Cada campeón puede tener hasta {LeagueChampion.MaxPhotos} fotografías.");
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return LeagueChampionMapper.ToDetail(champion);
        }
    }

    public sealed class UpdateLeagueChampionGalleryUseCase : IUpdateLeagueChampionGalleryUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLeagueChampionGalleryUseCase(
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<LeagueChampionDetailDto> ExecuteAsync(
            Guid leagueId,
            Guid championId,
            Guid userId,
            IReadOnlyList<LeagueChampionPhotoWrite> photos,
            CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, leagueId, championId, cancellationToken);
            var updates = (photos ?? Array.Empty<LeagueChampionPhotoWrite>())
                .Select(p => new LeagueChampionPhotoUpdate(p.Id, p.Caption, p.DisplayOrder, p.IsCover))
                .ToList();

            // Reject an invalid gallery before writing. Then clear covers and save before
            // assigning the new one: PostgreSQL checks the partial unique index per statement.
            LeagueChampionGallery.ApplyGalleryOrThrow(champion, updates, validateOnly: true);
            var withoutCover = updates
                .Select(p => new LeagueChampionPhotoUpdate(p.PhotoId, p.Caption, p.DisplayOrder, false))
                .ToList();
            LeagueChampionGallery.ApplyGalleryOrThrow(champion, withoutCover);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            LeagueChampionGallery.ApplyGalleryOrThrow(champion, updates);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return LeagueChampionMapper.ToDetail(champion);
        }
    }

    internal static class LeagueChampionGallery
    {
        public static void ApplyGalleryOrThrow(
            LeagueChampion champion,
            IReadOnlyList<LeagueChampionPhotoUpdate> updates,
            bool validateOnly = false)
        {
            try
            {
                if (validateOnly)
                    champion.EnsureGallery(updates);
                else
                    champion.ApplyGallery(updates);
            }
            catch (ArgumentException ex) when (ex.Message.Contains("cover", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("Solo puede haber una fotografía principal.");
            }
            catch (ArgumentException ex) when (ex.Message.Contains("Caption", StringComparison.Ordinal))
            {
                throw new BusinessException($"La descripción no puede superar {LeagueChampionPhoto.MaxCaptionLength} caracteres.");
            }
            catch (ArgumentException)
            {
                throw new BusinessException("La galería tiene que incluir todas las fotografías actuales, una sola vez.");
            }
        }
    }

    public sealed class DeleteLeagueChampionPhotoUseCase : IDeleteLeagueChampionPhotoUseCase
    {
        private readonly ILeagueChampionRepository _champions;
        private readonly IUserLeagueRepository _userLeagues;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLeagueChampionPhotoUseCase(
            ILeagueChampionRepository champions,
            IUserLeagueRepository userLeagues,
            IUnitOfWork unitOfWork)
        {
            _champions = champions ?? throw new ArgumentNullException(nameof(champions));
            _userLeagues = userLeagues ?? throw new ArgumentNullException(nameof(userLeagues));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<string> ExecuteAsync(Guid leagueId, Guid championId, Guid photoId, Guid userId, CancellationToken cancellationToken = default)
        {
            await LeagueChampionAccess.EnsureMemberAsync(_userLeagues, userId, leagueId, cancellationToken);
            var champion = await LeagueChampionAccess.RequireInLeagueAsync(_champions, leagueId, championId, cancellationToken);

            string imageUrl;
            try
            {
                imageUrl = champion.RemovePhoto(photoId);
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException("No se encontró la fotografía.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return imageUrl;
        }
    }

    internal static class LeagueChampionAccess
    {
        public static async Task EnsureMemberAsync(
            IUserLeagueRepository userLeagues,
            Guid userId,
            Guid leagueId,
            CancellationToken cancellationToken)
        {
            if (!await userLeagues.IsUserInLeagueAsync(userId, leagueId, cancellationToken))
                throw new ForbiddenAccessException("No tenés acceso a esta liga.");
        }

        public static async Task<LeagueChampion> RequireInLeagueAsync(
            ILeagueChampionRepository champions,
            Guid leagueId,
            Guid championId,
            CancellationToken cancellationToken)
        {
            var champion = await champions.GetByIdAsync(championId, cancellationToken);
            if (champion == null)
                throw new KeyNotFoundException("No se encontró el campeón.");
            if (champion.LeagueId != leagueId)
                throw new ForbiddenAccessException("El campeón no pertenece a esta liga.");
            return champion;
        }
    }
}
