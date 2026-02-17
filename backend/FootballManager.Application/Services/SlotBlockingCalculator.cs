using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services
{
    /// <summary>
    /// Use when generating fixtures: compute how many minutes to block on a field for a match.
    /// Tolerance applies only to the first scheduled match of each division per round; it affects
    /// slot blocking only (next match cannot start before this window ends). It does not change
    /// stored match duration or displayed start_time.
    /// </summary>
    public static class SlotBlockingCalculator
    {
        /// <summary>
        /// Base duration: (half_minutes * 2) + break_minutes + warmup_buffer_minutes.
        /// </summary>
        public static int GetBaseDurationMinutes(MatchRule rule)
        {
            if (rule == null) return 0;
            return (rule.HalfMinutes * 2) + rule.BreakMinutes + rule.WarmupBufferMinutes;
        }

        /// <summary>
        /// Total minutes to block for this match. If isFirstMatchOfDivisionInRound, adds first_match_tolerance_minutes.
        /// Use this to ensure the next match on the same field cannot start before startTime + (return value) minutes.
        /// </summary>
        public static int GetSlotBlockingDurationMinutes(MatchRule rule, bool isFirstMatchOfDivisionInRound)
        {
            var baseDuration = GetBaseDurationMinutes(rule);
            if (!isFirstMatchOfDivisionInRound) return baseDuration;
            return baseDuration + (rule?.FirstMatchToleranceMinutes ?? 0);
        }

        /// <summary>
        /// Validates that nextStartTime is not before the end of the slot blocking window.
        /// Both times are on the same day; durationMinutes is from GetSlotBlockingDurationMinutes.
        /// </summary>
        public static bool IsNextStartValid(TimeOnly currentStart, int slotBlockingDurationMinutes, TimeOnly nextStartTime)
        {
            var endBlock = currentStart.Add(TimeSpan.FromMinutes(slotBlockingDurationMinutes));
            return nextStartTime >= endBlock;
        }

        /// <summary>
        /// Rounds minutes to slot granularity (e.g. 5). Use when computing next slot start.
        /// </summary>
        public static int RoundToGranularity(int minutes, int slotGranularityMinutes)
        {
            if (slotGranularityMinutes <= 0) return minutes;
            return ((minutes + slotGranularityMinutes - 1) / slotGranularityMinutes) * slotGranularityMinutes;
        }
    }
}
