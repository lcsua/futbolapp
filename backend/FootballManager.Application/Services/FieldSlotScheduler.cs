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

    public int TotalMatchDurationMinutes => _totalMatchDurationMinutes;
}
