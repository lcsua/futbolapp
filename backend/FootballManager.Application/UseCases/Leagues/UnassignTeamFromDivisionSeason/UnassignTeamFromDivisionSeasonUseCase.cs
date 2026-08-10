using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UnassignTeamFromDivisionSeason
{
    public class UnassignTeamFromDivisionSeasonUseCase : IUnassignTeamFromDivisionSeasonUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IFixtureRepository _fixtureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UnassignTeamFromDivisionSeasonUseCase(
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            ITeamRepository teamRepository,
            ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IFixtureRepository fixtureRepository,
            IUnitOfWork unitOfWork)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UnassignTeamFromDivisionSeasonRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            SeasonGuard.EnsureOpen(season);

            var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken);
            if (division == null)
                throw new KeyNotFoundException($"Division {request.DivisionId} not found.");
            if (division.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Division does not belong to this league.");

            var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
            if (team == null)
                throw new KeyNotFoundException($"Team {request.TeamId} not found.");
            if (team.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Team does not belong to this league.");

            var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(
                request.SeasonId, request.DivisionId, cancellationToken);
            if (divisionSeason == null)
                throw new KeyNotFoundException($"Division \"{division.Name}\" is not assigned to this season.");

            var fixtureCount = await _fixtureRepository.CountByDivisionSeasonIdAsync(divisionSeason.Id, cancellationToken);
            if (fixtureCount > 0)
            {
                throw new BusinessException(
                    $"Cannot modify team assignment for division \"{division.Name}\": fixtures have been committed for that division.");
            }

            var removed = await _teamDivisionSeasonRepository.RemoveByTeamAndDivisionSeasonAsync(
                request.TeamId, divisionSeason.Id, cancellationToken);
            if (!removed)
                throw new KeyNotFoundException($"Team {team.Name} is not assigned to division \"{division.Name}\" in this season.");

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
