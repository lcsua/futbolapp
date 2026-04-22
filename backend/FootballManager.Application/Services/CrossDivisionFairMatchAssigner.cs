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
            .ToList();

        var groupedByDivision = indexed
            .GroupBy(x => x.Ds.Id)
            .OrderByDescending(g => g.Max(x => x.Rules.TotalMatchSlotBlockMinutes + x.Rules.FirstMatchToleranceMinutes))
            .ThenBy(g => g.Key)
            .Select(g => g.ToList())
            .ToList();

        var outputs = new (Guid FieldId, DateOnly Date, TimeOnly Start)?[matches.Count];
        var occupations = new List<FieldOccupation>();
        var preferredKickoffByDivision = new Dictionary<Guid, TimeOnly>();
        var divisionFieldSeenInRound = new HashSet<(Guid DivisionSeasonId, Guid FieldId)>();

        foreach (var divisionMatches in groupedByDivision)
        {
            foreach (var (origIdx, ds, home, away, rules) in divisionMatches)
            {
                var divisionId = ds.DivisionId;
                var divisionSeasonId = ds.Id;

                var allowedFieldSet = rules.AllowedFieldIds == null ? null : new HashSet<Guid>(rules.AllowedFieldIds);

                var candidates = new List<(Guid FieldId, TimeOnly Start, int Score, int ReserveEndOffset)>();

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
                        var isFirstForDivisionOnField = !divisionFieldSeenInRound.Contains((divisionSeasonId, avail.FieldId));
                        var blockingMinutes = SlotBlockingCalculator.GetSlotBlockingDurationMinutes(rules, isFirstForDivisionOnField);
                        var reserveEndOffset = blockingMinutes + rules.BreakBetweenMatchesMinutes;
                        var endExclusiveMin = startMin + reserveEndOffset;
                        if (Overlaps(avail.FieldId, matchDate, startMin, endExclusiveMin, occupations))
                        {
                            current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                            continue;
                        }

                        var score = teamFieldUsage.GetUsage(home.Team.Id, avail.FieldId)
                                    + teamFieldUsage.GetUsage(away.Team.Id, avail.FieldId);
                        candidates.Add((avail.FieldId, current, score, reserveEndOffset));
                        current = FieldSlotScheduler.CeilTimeToGranularity(current.AddMinutes(gran), gran);
                    }
                }

                if (candidates.Count == 0)
                    return null;

                var chosen = ChooseBestCandidateForDivision(
                    candidates,
                    preferredKickoffByDivision.TryGetValue(divisionSeasonId, out var preferredKickoff) ? preferredKickoff : null,
                    random);

                if (!preferredKickoffByDivision.ContainsKey(divisionSeasonId) || chosen.Start != preferredKickoffByDivision[divisionSeasonId])
                    preferredKickoffByDivision[divisionSeasonId] = chosen.Start;

                var startMinChosen = ToMinutesFromMidnight(chosen.Start);
                occupations.Add(new FieldOccupation(chosen.FieldId, matchDate, startMinChosen, startMinChosen + chosen.ReserveEndOffset));
                divisionFieldSeenInRound.Add((divisionSeasonId, chosen.FieldId));
                teamFieldUsage.AddUsage(home.Team.Id, chosen.FieldId);
                teamFieldUsage.AddUsage(away.Team.Id, chosen.FieldId);
                outputs[origIdx] = (chosen.FieldId, matchDate, chosen.Start);
            }
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

    /// <summary>
    /// Prefer synchronized kickoff per division when possible:
    /// 1) preferred kickoff (current wave) + best score,
    /// 2) otherwise earliest available kickoff + best score.
    /// </summary>
    private static (Guid FieldId, TimeOnly Start, int ReserveEndOffset) ChooseBestCandidateForDivision(
        IReadOnlyList<(Guid FieldId, TimeOnly Start, int Score, int ReserveEndOffset)> candidates,
        TimeOnly? preferredKickoff,
        Random random)
    {
        if (preferredKickoff.HasValue)
        {
            var sameStart = candidates.Where(c => c.Start == preferredKickoff.Value).ToList();
            if (sameStart.Count > 0)
            {
                var bestScoreSameStart = sameStart.Min(c => c.Score);
                var bestSameStart = sameStart.Where(c => c.Score == bestScoreSameStart).ToList();
                var chosenSameStart = bestSameStart[random.Next(bestSameStart.Count)];
                return (chosenSameStart.FieldId, chosenSameStart.Start, chosenSameStart.ReserveEndOffset);
            }
        }

        var earliestStart = candidates.Min(c => c.Start);
        var earliest = candidates.Where(c => c.Start == earliestStart).ToList();
        var bestScoreEarliest = earliest.Min(c => c.Score);
        var bestEarliest = earliest.Where(c => c.Score == bestScoreEarliest).ToList();
        var chosen = bestEarliest[random.Next(bestEarliest.Count)];
        return (chosen.FieldId, chosen.Start, chosen.ReserveEndOffset);
    }

    private static int ToMinutesFromMidnight(TimeOnly t) => t.Hour * 60 + t.Minute;
}
