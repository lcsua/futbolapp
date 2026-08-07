using System;
using System.Collections.Generic;

namespace FootballManager.Application.Helpers;

/// <summary>
/// Builds match dates for fixture rounds from an admin-chosen first-round date
/// and the competition match-day list (e.g. Saturday only → weekly).
/// </summary>
public static class FixtureRoundDateCalculator
{
    /// <summary>
    /// Round 0 uses <paramref name="firstRoundDate"/> exactly.
    /// Later rounds pick the next occurrence of the corresponding match day after the previous date.
    /// </summary>
    public static IReadOnlyList<DateOnly> BuildRoundDates(
        DateOnly firstRoundDate,
        IReadOnlyList<int> matchDaysSorted,
        int roundCount)
    {
        if (matchDaysSorted == null || matchDaysSorted.Count == 0)
            throw new ArgumentException("At least one match day is required.", nameof(matchDaysSorted));
        if (roundCount < 1)
            return Array.Empty<DateOnly>();

        var dates = new List<DateOnly>(roundCount) { firstRoundDate };
        for (var i = 1; i < roundCount; i++)
        {
            var targetDow = matchDaysSorted[i % matchDaysSorted.Count];
            dates.Add(NextDayOfWeekAfter(dates[i - 1], targetDow));
        }

        return dates;
    }

    /// <summary>
    /// Next calendar date strictly after <paramref name="after"/> whose DayOfWeek equals <paramref name="dayOfWeek"/> (0=Sun..6=Sat).
    /// </summary>
    public static DateOnly NextDayOfWeekAfter(DateOnly after, int dayOfWeek)
    {
        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week must be 0–6.");

        var candidate = after.AddDays(1);
        var current = (int)candidate.ToDateTime(TimeOnly.MinValue).DayOfWeek;
        var diff = (dayOfWeek - current + 7) % 7;
        return candidate.AddDays(diff);
    }
}
