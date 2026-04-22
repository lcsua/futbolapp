using System;
using System.Collections.Generic;
using System.Linq;
using FootballManager.Application.Dtos;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services;

/// <summary>
/// Assigns matches in a single round using per–division-season effective rules (duration, fields, kickoff windows).
/// Matches with longer slot blocks are scheduled first to reduce failures.
/// </summary>
public static class CrossDivisionFairMatchAssigner
{
    /// <summary>Returns one slot per match in the same order as <paramref name="matches"/>.</summary>
    public static IReadOnlyList<(Guid FieldId, DateOnly Date, TimeOnly StartTime)>? Assign(
        IReadOnlyList<(DivisionSeason Ds, TeamDivisionSeason Home, TeamDivisionSeason Away, EffectiveMatchRulesDto Rules)> matches,
        DateOnly matchDate,
        int dayOfWeek,
        IReadOnlyList<FieldAvailability> availabilities,
        IReadOnlyDictionary<Guid, Field> fieldsById,
        TeamFieldUsage teamFieldUsage,
        Func<Guid, TimeOnly, bool> isKickoffAllowedForDivision,
        Random? random = null)
    {
        if (matches.Count == 0)
            return Array.Empty<(Guid, DateOnly, TimeOnly)>();

        random ??= Random.Shared;

        var indexed = matches
            .Select((m, i) => (Idx: i, m.Ds, m.Home, m.Away, m.Rules))
            .OrderByDescending(x => x.Rules.TotalMatchSlotBlockMinutes + x.Rules.FirstMatchToleranceMinutes)
            .ToList();

        var outputs = new (Guid FieldId, DateOnly Date, TimeOnly Start)?[matches.Count];
        var divisionSeenInRound = new HashSet<Guid>();
        var occupations = new List<FieldOccupation>();

        foreach (var (origIdx, ds, home, away, rules) in indexed)
        {
            var divisionId = ds.DivisionId;
            var isFirstForDivision = !divisionSeenInRound.Contains(ds.Id);
            divisionSeenInRound.Add(ds.Id);

            var blockingMinutes = SlotBlockingCalculator.GetSlotBlockingDurationMinutes(rules, isFirstForDivision);
            var reserveEndOffset = blockingMinutes + rules.BreakBetweenMatchesMinutes;

            var allowedFieldSet = rules.AllowedFieldIds == null ? null : new HashSet<Guid>(rules.AllowedFieldIds);

            var bestScore = int.MaxValue;
            var bestCandidates = new List<(Guid FieldId, TimeOnly Start)>();

            foreach (var avail in availabilities.Where(a => a.DayOfWeek == dayOfWeek && a.IsActive))
            {
                if (!fieldsById.TryGetValue(avail.FieldId, out var field) || !field.IsAvailable)
                    continue;
                if (allowedFieldSet != null && !allowedFieldSet.Contains(avail.FieldId))
                    continue;

                var gran = rules.SlotGranularityMinutes;
                var current = FieldSlotScheduler.CeilTimeToGranularity(avail.StartTime, gran);
                while (current.AddMinutes(rules.TotalMatchSlotBlockMinutes) <= avail.EndTime)
                {
                    if (!isKickoffAllowedForDivision(divisionId, current))
                    {
                        current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                        continue;
                    }

                    if (!KickoffInRanges(current, rules.AllowedKickoffTimeRanges))
                    {
                        current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                        continue;
                    }

                    var startMin = ToMinutesFromMidnight(current);
                    var endExclusiveMin = startMin + reserveEndOffset;
                    if (Overlaps(avail.FieldId, matchDate, startMin, endExclusiveMin, occupations))
                    {
                        current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                        continue;
                    }

                    var score = teamFieldUsage.GetUsage(home.Team.Id, avail.FieldId)
                                + teamFieldUsage.GetUsage(away.Team.Id, avail.FieldId);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestCandidates.Clear();
                        bestCandidates.Add((avail.FieldId, current));
                    }
                    else if (score == bestScore)
                    {
                        bestCandidates.Add((avail.FieldId, current));
                    }

                    current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                }
            }

            if (bestCandidates.Count == 0)
                return null;

            var chosen = bestCandidates[random.Next(bestCandidates.Count)];
            var startMinChosen = ToMinutesFromMidnight(chosen.Start);
            occupations.Add(new FieldOccupation(chosen.FieldId, matchDate, startMinChosen, startMinChosen + reserveEndOffset));
            teamFieldUsage.AddUsage(home.Team.Id, chosen.FieldId);
            teamFieldUsage.AddUsage(away.Team.Id, chosen.FieldId);
            outputs[origIdx] = (chosen.FieldId, matchDate, chosen.Start);
        }

        return outputs.Select(o => o!.Value).ToList();
    }

    private readonly record struct FieldOccupation(Guid FieldId, DateOnly Date, int StartMin, int EndExclusiveMin);

    private static bool Overlaps(Guid fieldId, DateOnly date, int startMin, int endExclusiveMin, List<FieldOccupation> occupations)
    {
        foreach (var o in occupations)
        {
            if (o.FieldId != fieldId || o.Date != date) continue;
            if (startMin < o.EndExclusiveMin && o.StartMin < endExclusiveMin)
                return true;
        }

        return false;
    }

    private static bool KickoffInRanges(TimeOnly kickoff, IReadOnlyList<EffectiveKickoffTimeRangeDto>? ranges)
    {
        if (ranges == null || ranges.Count == 0) return true;
        return ranges.Any(r => kickoff >= r.Start && kickoff < r.End);
    }

    private static int ToMinutesFromMidnight(TimeOnly t) => t.Hour * 60 + t.Minute;
}
