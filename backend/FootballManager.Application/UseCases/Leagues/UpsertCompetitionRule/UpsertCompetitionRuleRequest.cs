using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.UpsertCompetitionRule
{
    public class UpsertCompetitionRuleRequest
    {
        public Guid LeagueId { get; set; }
        public Guid? SeasonId { get; set; }
        public Guid UserId { get; set; }
        public int MatchesPerWeek { get; set; } = 1;
        public bool IsHomeAway { get; set; }
        public List<int> MatchDays { get; set; } = new();
    }
}
