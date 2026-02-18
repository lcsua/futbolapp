using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetTeamIdsAssignedToSeason
{
    public class GetTeamIdsAssignedToSeasonUseCase : IGetTeamIdsAssignedToSeasonUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetTeamIdsAssignedToSeasonUseCase(
            ISeasonRepository seasonRepository,
            ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetTeamIdsAssignedToSeasonResponse> ExecuteAsync(GetTeamIdsAssignedToSeasonRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            var teamIds = await _teamDivisionSeasonRepository.GetTeamIdsAssignedToSeasonAsync(request.SeasonId, cancellationToken);
            return new GetTeamIdsAssignedToSeasonResponse(teamIds);
        }
    }
}
