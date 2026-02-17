using System;

namespace FootballManager.Application.UseCases.Leagues.GetCompetitionRule
{
    public class GetCompetitionRuleRequest
    {
        public Guid LeagueId { get; }
        public Guid? SeasonId { get; }
        public Guid UserId { get; }

        public GetCompetitionRuleRequest(Guid leagueId, Guid userId, Guid? seasonId = null)
        {
            LeagueId = leagueId;
            SeasonId = seasonId;
            UserId = userId;
        }
    }
}
