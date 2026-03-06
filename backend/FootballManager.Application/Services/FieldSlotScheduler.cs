using System;
using System.Collections.Generic;
using System.Linq;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services;

/// <summary>
/// Represents a slot where a match can be scheduled. Based on field_availability windows.
/// </summary>
public sealed record FieldSlot(Guid FieldId, DateOnly Date, TimeOnly StartTime);

/// <summary>
/// Tracks how many times each team has played on each field. Key is (TeamId, FieldId).
/// </summary>
public sealed class TeamFieldUsage
{
    private readonly Dictionary<(Guid TeamId, Guid FieldId), int> _usage = new();

    public int GetUsage(Guid teamId, Guid fieldId)
    {
        return _usage.TryGetValue((teamId, fieldId), out var count) ? count : 0;
    }

    public void AddUsage(Guid teamId, Guid fieldId)
    {
        var key = (teamId, fieldId);
        _usage[key] = GetUsage(teamId, fieldId) + 1;
    }

    /// <summary>
    /// Returns a snapshot of (TeamId, FieldId) -> count for validation/logging.
    /// </summary>
    public IReadOnlyDictionary<(Guid TeamId, Guid FieldId), int> Snapshot()
    {
        return new Dictionary<(Guid TeamId, Guid FieldId), int>(_usage);
    }
}

/// <summary>
/// Generates match slots from field_availability windows. Slots are only created when
/// a field is available and the match fits within the availability window.
/// </summary>
public sealed class FieldSlotScheduler
{
    private readonly int _slotGranularityMinutes;
    private readonly int _totalMatchDurationMinutes;

    public FieldSlotScheduler(int slotGranularityMinutes, int totalMatchDurationMinutes)
    {
        _slotGranularityMinutes = Math.Max(1, slotGranularityMinutes);
        _totalMatchDurationMinutes = Math.Max(1, totalMatchDurationMinutes);
    }

    /// <summary>
    /// Generate slots for a matchday from field availability. Only creates slots when:
    /// - availability.day_of_week matches the matchday's weekday
    /// - Match duration fits within the window (start + duration <= end_time)
    /// </summary>
    /// <param name="matchDate">The date of the matchday</param>
    /// <param name="dayOfWeek">Day of week (0=Sunday, 6=Saturday) for the matchdate</param>
    /// <param name="availabilities">All field availabilities for the league's fields, filtered by day if needed</param>
    /// <returns>Slots sorted by date, time, field - ready for sequential match assignment</returns>
    public IReadOnlyList<FieldSlot> GenerateSlotsForMatchday(
        DateOnly matchDate,
        int dayOfWeek,
        IReadOnlyList<Domain.Entities.FieldAvailability> availabilities)
    {
        var slots = new List<FieldSlot>();

        foreach (var avail in availabilities.Where(a => a.DayOfWeek == dayOfWeek && a.IsActive))
        {
            var start = avail.StartTime;
            var end = avail.EndTime;

            if (start.AddMinutes(_totalMatchDurationMinutes) > end)
                continue;

            var current = start;
            while (current.AddMinutes(_totalMatchDurationMinutes) <= end)
            {
                slots.Add(new FieldSlot(avail.FieldId, matchDate, current));
                current = current.Add(TimeSpan.FromMinutes(_totalMatchDurationMinutes));
            }
        }

        return slots
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => s.FieldId)
            .ToList();
    }

    /// <summary>
    /// Assign matches to slots sequentially. Returns (FieldId, Date, StartTime) for each match.
    /// </summary>
    public IReadOnlyList<(Guid FieldId, DateOnly Date, TimeOnly StartTime)> AssignMatchesToSlots(
        int matchCount,
        IReadOnlyList<FieldSlot> slots)
    {
        if (slots.Count < matchCount)
            return null;

        var result = new List<(Guid, DateOnly, TimeOnly)>(matchCount);
        for (var i = 0; i < matchCount; i++)
        {
            var slot = slots[i];
            result.Add((slot.FieldId, slot.Date, slot.StartTime));
        }
        return result;
    }

    /// <summary>
    /// Assigns matches to slots using fair field distribution: picks the field with the lowest
    /// combined usage for both teams (team A usage + team B usage on that field).
    /// Only considers slots that are in the given list (already respect field_availability).
    /// </summary>
    /// <param name="matches">Each element is (HomeTeamId, AwayTeamId)</param>
    /// <param name="slots">Available slots from GenerateSlotsForMatchday (respects field_availability)</param>
    /// <param name="teamFieldUsage">Running usage counts; updated as assignments are made</param>
    /// <param name="random">Optional RNG for shuffling slot order and tie-breaking</param>
    /// <returns>One (FieldId, Date, StartTime) per match, or null if not enough slots</returns>
    public IReadOnlyList<(Guid FieldId, DateOnly Date, TimeOnly StartTime)> AssignMatchesToSlotsWithFairness(
        IReadOnlyList<(Guid HomeTeamId, Guid AwayTeamId)> matches,
        IReadOnlyList<FieldSlot> slots,
        TeamFieldUsage teamFieldUsage,
        Random random = null)
    {
        if (matches == null || matches.Count == 0)
            return new List<(Guid, DateOnly, TimeOnly)>();
        if (slots == null || slots.Count < matches.Count)
            return null;

        random ??= Random.Shared;
        var slotCount = slots.Count;
        var availableIndices = new List<int>(slotCount);
        for (var i = 0; i < slotCount; i++)
            availableIndices.Add(i);
        Shuffle(availableIndices, random);

        var result = new List<(Guid FieldId, DateOnly Date, TimeOnly StartTime)>(matches.Count);

        foreach (var (homeTeamId, awayTeamId) in matches)
        {
            var bestScore = int.MaxValue;
            var candidates = new List<int>();

            foreach (var idx in availableIndices)
            {
                var slot = slots[idx];
                var score = teamFieldUsage.GetUsage(homeTeamId, slot.FieldId)
                            + teamFieldUsage.GetUsage(awayTeamId, slot.FieldId);
                if (score < bestScore)
                {
                    bestScore = score;
                    candidates.Clear();
                    candidates.Add(idx);
                }
                else if (score == bestScore)
                {
                    candidates.Add(idx);
                }
            }

            if (candidates.Count == 0)
                return null;

            var chosenIdx = candidates[random.Next(candidates.Count)];
            var chosenSlot = slots[chosenIdx];
            result.Add((chosenSlot.FieldId, chosenSlot.Date, chosenSlot.StartTime));
            availableIndices.Remove(chosenIdx);
            teamFieldUsage.AddUsage(homeTeamId, chosenSlot.FieldId);
            teamFieldUsage.AddUsage(awayTeamId, chosenSlot.FieldId);
        }

        return result;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    public int TotalMatchDurationMinutes => _totalMatchDurationMinutes;
}
