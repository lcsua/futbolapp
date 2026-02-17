using System;

namespace FootballManager.Application.UseCases.Leagues.UpsertMatchRule
{
    public class UpsertMatchRuleRequest
    {
        public Guid LeagueId { get; set; }
        public Guid? SeasonId { get; set; }
        public Guid UserId { get; set; }
        public int HalfMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public int WarmupBufferMinutes { get; set; }
        public int SlotGranularityMinutes { get; set; }
        public int FirstMatchToleranceMinutes { get; set; }
    }
}
