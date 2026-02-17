using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.AssignTeamToDivisionSeason
{
    public class AssignTeamToDivisionSeasonUseCase : IAssignTeamToDivisionSeasonUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignTeamToDivisionSeasonUseCase(
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            ITeamRepository teamRepository,
            ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<AssignTeamToDivisionSeasonResponse> ExecuteAsync(AssignTeamToDivisionSeasonRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

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

            var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(request.SeasonId, request.DivisionId, cancellationToken);
            if (divisionSeason == null)
            {
                divisionSeason = new DivisionSeason(season, division);
                await _divisionSeasonRepository.AddAsync(divisionSeason, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (await _teamDivisionSeasonRepository.ExistsAsync(request.TeamId, divisionSeason.Id, cancellationToken))
                throw new InvalidOperationException("Team is already assigned to this division in this season.");

            var assignment = new TeamDivisionSeason(team, divisionSeason);
            await _teamDivisionSeasonRepository.AddAsync(assignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AssignTeamToDivisionSeasonResponse(assignment.Id);
        }
    }
}
