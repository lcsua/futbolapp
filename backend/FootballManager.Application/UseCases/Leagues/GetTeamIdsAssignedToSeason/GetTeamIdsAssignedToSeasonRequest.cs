using System;

namespace FootballManager.Application.UseCases.Leagues.GetTeamIdsAssignedToSeason
{
    public class GetTeamIdsAssignedToSeasonRequest
    {
        public Guid LeagueId { get; }
        public Guid SeasonId { get; }
        public Guid UserId { get; }

        public GetTeamIdsAssignedToSeasonRequest(Guid leagueId, Guid seasonId, Guid userId)
        {
            LeagueId = leagueId;
            SeasonId = seasonId;
            UserId = userId;
        }
    }
}
