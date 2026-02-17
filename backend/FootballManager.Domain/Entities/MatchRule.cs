using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class MatchRule : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public Guid? SeasonId { get; private set; }
        public virtual Season Season { get; private set; }

        public int HalfMinutes { get; private set; }
        public int BreakMinutes { get; private set; }
        public int WarmupBufferMinutes { get; private set; }
        public int SlotGranularityMinutes { get; private set; }

        protected MatchRule() { }

        public MatchRule(League league, int halfMinutes, int breakMinutes, int warmupBufferMinutes = 0, int slotGranularityMinutes = 5, Season season = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            SeasonId = season?.Id;
            Season = season;
            HalfMinutes = halfMinutes;
            BreakMinutes = breakMinutes;
            WarmupBufferMinutes = warmupBufferMinutes;
            SlotGranularityMinutes = slotGranularityMinutes;
        }

        public void UpdateDetails(int halfMinutes, int breakMinutes, int warmupBufferMinutes, int slotGranularityMinutes)
        {
            HalfMinutes = halfMinutes;
            BreakMinutes = breakMinutes;
            WarmupBufferMinutes = warmupBufferMinutes;
            SlotGranularityMinutes = slotGranularityMinutes;
            UpdateTimestamp();
        }
    }
}
