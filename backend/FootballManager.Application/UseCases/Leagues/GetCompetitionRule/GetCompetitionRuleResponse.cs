using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.GetCompetitionRule
{
    public class GetCompetitionRuleResponse
    {
        public Guid Id { get; }
        public Guid LeagueId { get; }
        public Guid? SeasonId { get; }
        public int MatchesPerWeek { get; }
        public bool IsHomeAway { get; }
        public List<int> MatchDays { get; }

        public GetCompetitionRuleResponse(Guid id, Guid leagueId, Guid? seasonId, int matchesPerWeek, bool isHomeAway, List<int> matchDays)
        {
            Id = id;
            LeagueId = leagueId;
            SeasonId = seasonId;
            MatchesPerWeek = matchesPerWeek;
            IsHomeAway = isHomeAway;
            MatchDays = matchDays ?? new List<int>();
        }
    }
}
