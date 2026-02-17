using System;

namespace FootballManager.Application.UseCases.Leagues.GetMatchRule
{
    public class GetMatchRuleResponse
    {
        public Guid Id { get; }
        public Guid LeagueId { get; }
        public Guid? SeasonId { get; }
        public int HalfMinutes { get; }
        public int BreakMinutes { get; }
        public int WarmupBufferMinutes { get; }
        public int SlotGranularityMinutes { get; }
        public int FirstMatchToleranceMinutes { get; }

        public GetMatchRuleResponse(Guid id, Guid leagueId, Guid? seasonId, int halfMinutes, int breakMinutes, int warmupBufferMinutes, int slotGranularityMinutes, int firstMatchToleranceMinutes)
        {
            Id = id;
            LeagueId = leagueId;
            SeasonId = seasonId;
            HalfMinutes = halfMinutes;
            BreakMinutes = breakMinutes;
            WarmupBufferMinutes = warmupBufferMinutes;
            SlotGranularityMinutes = slotGranularityMinutes;
            FirstMatchToleranceMinutes = firstMatchToleranceMinutes;
        }
    }
}
