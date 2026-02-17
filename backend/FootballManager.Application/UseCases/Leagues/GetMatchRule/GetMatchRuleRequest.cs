using System;

namespace FootballManager.Application.UseCases.Leagues.GetMatchRule
{
    public class GetMatchRuleRequest
    {
        public Guid LeagueId { get; }
        public Guid? SeasonId { get; }
        public Guid UserId { get; }

        public GetMatchRuleRequest(Guid leagueId, Guid userId, Guid? seasonId = null)
        {
            LeagueId = leagueId;
            SeasonId = seasonId;
            UserId = userId;
        }
    }
}
