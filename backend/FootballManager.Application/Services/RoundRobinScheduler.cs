using System;
using System.Collections.Generic;
using System.Linq;

namespace FootballManager.Application.Services;

/// <summary>
/// Round-robin: circle method with optional phantom team for odd counts (one bye per round).
/// Localía alternates between matchdays: after home, the next match prefers away (and vice versa);
/// a bye does not change that preference. This applies whether or not <see cref="isHomeAway"/> is set.
/// <see cref="isHomeAway"/> only adds return fixtures (same pairings); the return leg is always
/// the opposite venue from the first leg between the same two teams.
/// </summary>
public static class RoundRobinScheduler
{
    /// <summary>
    /// Generate round-robin pairings. Each inner list is a round of (home, away) indices in 0..N-1.
    /// Odd N: one team rests each round (no match row for that team).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<(int Home, int Away)>> Generate(int teamCount, bool isHomeAway)
    {
        if (teamCount < 2)
            return Array.Empty<IReadOnlyList<(int, int)>>();

        var unorderedFirstHalf = BuildUnorderedHalfRounds(teamCount);
        var allUnordered = new List<IReadOnlyList<(int Min, int Max)>>(unorderedFirstHalf);
        if (isHomeAway)
            allUnordered.AddRange(unorderedFirstHalf);

        return AssignHomeAwayAlternating(allUnordered, teamCount);
    }

    /// <summary>
    /// Single round-robin half: N-1 rounds (N even) or N rounds (N odd, standard circle on N+1 with bye).
    /// Each round is a list of unordered pairs (min,max) with both indices in [0, teamCount-1].
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<(int Min, int Max)>> BuildUnorderedHalfRounds(int teamCount)
    {
        var n = teamCount % 2 == 0 ? teamCount : teamCount + 1;
        var matchesPerRound = n / 2;
        var halfRounds = n - 1;

        var ring = Enumerable.Range(1, n - 1).ToArray();
        var rounds = new List<IReadOnlyList<(int Min, int Max)>>(halfRounds);

        for (var r = 0; r < halfRounds; r++)
        {
            var edges = new List<(int Min, int Max)>(matchesPerRound);
            TryAddEdge(edges, teamCount, 0, ring[0]);
            for (var i = 1; i < matchesPerRound; i++)
                TryAddEdge(edges, teamCount, ring[i], ring[ring.Length - i]);

            rounds.Add(edges);

            if (r < halfRounds - 1)
            {
                var last = ring[^1];
                Array.Copy(ring, 0, ring, 1, ring.Length - 1);
                ring[0] = last;
            }
        }

        return rounds;
    }

    /// <summary>
    /// Adds a match (min,max) if both indices are real teams; skips pairs involving the phantom (bye).
    /// </summary>
    private static void TryAddEdge(List<(int Min, int Max)> edges, int teamCount, int a, int b)
    {
        if (a == b) return;
        if (a >= teamCount || b >= teamCount) return;

        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        edges.Add((min, max));
    }

    /// <summary>
    /// Return legs: forced opposite venue from first leg. Otherwise: per pair, pick the orientation
    /// that best satisfies alternation (each pair is independent within the same round).
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<(int Home, int Away)>> AssignHomeAwayAlternating(
        IReadOnlyList<IReadOnlyList<(int Min, int Max)>> unorderedRounds,
        int teamCount)
    {
        var lastWasHome = new bool?[teamCount];
        var sameVenueStreak = new int[teamCount];
        var homeCounts = new int[teamCount];
        var awayCounts = new int[teamCount];
        var firstLegMinWasHome = new Dictionary<(int Min, int Max), bool>();

        foreach (var round in unorderedRounds)
        {
            foreach (var (min, max) in round)
            {
                int home;
                int away;

                if (firstLegMinWasHome.TryGetValue((min, max), out var minWasHomeFirstLeg))
                {
                    home = minWasHomeFirstLeg ? max : min;
                    away = minWasHomeFirstLeg ? min : max;
                }
                else
                {
                    // Independiente por pareja:
                    // 1) minimizar repeticiones consecutivas (H-H / A-A) cuando exista alternativa;
                    // 2) luego minimizar racha y desbalance acumulado.
                    var minHomeMetrics = EvaluatePairOrientation(lastWasHome, sameVenueStreak, homeCounts, awayCounts, min, max, minIsHome: true);
                    var maxHomeMetrics = EvaluatePairOrientation(lastWasHome, sameVenueStreak, homeCounts, awayCounts, min, max, minIsHome: false);

                    if (minHomeMetrics.RepeatedCount < maxHomeMetrics.RepeatedCount)
                    {
                        home = min;
                        away = max;
                    }
                    else if (maxHomeMetrics.RepeatedCount < minHomeMetrics.RepeatedCount)
                    {
                        home = max;
                        away = min;
                    }
                    else if (minHomeMetrics.MaxProjectedStreak < maxHomeMetrics.MaxProjectedStreak)
                    {
                        home = min;
                        away = max;
                    }
                    else if (maxHomeMetrics.MaxProjectedStreak < minHomeMetrics.MaxProjectedStreak)
                    {
                        home = max;
                        away = min;
                    }
                    else if (minHomeMetrics.BalancePenalty < maxHomeMetrics.BalancePenalty)
                    {
                        home = min;
                        away = max;
                    }
                    else if (maxHomeMetrics.BalancePenalty < minHomeMetrics.BalancePenalty)
                    {
                        home = max;
                        away = min;
                    }
                    else
                    {
                        var preferMinHome = IsMinHomeBetterForBalance(min, max, homeCounts, awayCounts);
                        home = preferMinHome ? min : max;
                        away = preferMinHome ? max : min;
                    }

                    firstLegMinWasHome[(min, max)] = home == min;
                }

                UpdateTeamVenueTracking(home, isHome: true, lastWasHome, sameVenueStreak, homeCounts, awayCounts);
                UpdateTeamVenueTracking(away, isHome: false, lastWasHome, sameVenueStreak, homeCounts, awayCounts);
            }
        }

        var optimizedFirstLeg = OptimizeFirstLegOrientations(unorderedRounds, teamCount, firstLegMinWasHome);
        return BuildOrientedRounds(unorderedRounds, optimizedFirstLeg);
    }

    private static Dictionary<(int Min, int Max), bool> OptimizeFirstLegOrientations(
        IReadOnlyList<IReadOnlyList<(int Min, int Max)>> unorderedRounds,
        int teamCount,
        IReadOnlyDictionary<(int Min, int Max), bool> seed)
    {
        var current = seed.ToDictionary(x => x.Key, x => x.Value);
        if (current.Count == 0) return current;

        var currentPenalty = EvaluateSchedulePenalty(unorderedRounds, teamCount, current);
        var improved = true;
        while (improved)
        {
            improved = false;
            var bestPair = ((int Min, int Max)?)null;
            var bestPenalty = currentPenalty;

            foreach (var pair in current.Keys.ToList())
            {
                current[pair] = !current[pair];
                var p = EvaluateSchedulePenalty(unorderedRounds, teamCount, current);
                current[pair] = !current[pair];

                if (p < bestPenalty)
                {
                    bestPenalty = p;
                    bestPair = pair;
                }
            }

            if (bestPair.HasValue)
            {
                current[bestPair.Value] = !current[bestPair.Value];
                currentPenalty = bestPenalty;
                improved = true;
            }
        }

        return current;
    }

    private static IReadOnlyList<IReadOnlyList<(int Home, int Away)>> BuildOrientedRounds(
        IReadOnlyList<IReadOnlyList<(int Min, int Max)>> unorderedRounds,
        IReadOnlyDictionary<(int Min, int Max), bool> firstLegMinWasHome)
    {
        var seenPairCount = new Dictionary<(int Min, int Max), int>();
        var result = new List<IReadOnlyList<(int Home, int Away)>>(unorderedRounds.Count);
        foreach (var round in unorderedRounds)
        {
            var oriented = new List<(int Home, int Away)>(round.Count);
            foreach (var pair in round)
            {
                var count = seenPairCount.TryGetValue(pair, out var c) ? c + 1 : 1;
                seenPairCount[pair] = count;

                var minHomeFirstLeg = firstLegMinWasHome.GetValueOrDefault(pair, true);
                var minIsHome = count == 1 ? minHomeFirstLeg : !minHomeFirstLeg;
                oriented.Add(minIsHome ? (pair.Min, pair.Max) : (pair.Max, pair.Min));
            }
            result.Add(oriented);
        }
        return result;
    }

    private static int EvaluateSchedulePenalty(
        IReadOnlyList<IReadOnlyList<(int Min, int Max)>> unorderedRounds,
        int teamCount,
        IReadOnlyDictionary<(int Min, int Max), bool> firstLegMinWasHome)
    {
        var orientedRounds = BuildOrientedRounds(unorderedRounds, firstLegMinWasHome);
        var homeCounts = new int[teamCount];
        var awayCounts = new int[teamCount];
        var lastVenue = new int[teamCount]; // 0=none, 1=home, -1=away
        var streak = new int[teamCount];

        var repeatedPenalty = 0;
        var longStreakPenalty = 0;

        foreach (var round in orientedRounds)
        {
            foreach (var (home, away) in round)
            {
                homeCounts[home]++;
                awayCounts[away]++;

                repeatedPenalty += UpdateStreakPenalty(home, venue: 1, lastVenue, streak, ref longStreakPenalty);
                repeatedPenalty += UpdateStreakPenalty(away, venue: -1, lastVenue, streak, ref longStreakPenalty);
            }
            // Bye does not reset venue preference/streak.
        }

        var balancePenalty = 0;
        for (var t = 0; t < teamCount; t++)
            balancePenalty += Math.Abs(homeCounts[t] - awayCounts[t]);

        // Lexicographic-like weighting: first avoid repeats, then long streaks, then overall balance.
        return (repeatedPenalty * 10_000) + (longStreakPenalty * 1_000) + balancePenalty;
    }

    private static int UpdateStreakPenalty(
        int team,
        int venue,
        int[] lastVenue,
        int[] streak,
        ref int longStreakPenalty)
    {
        if (lastVenue[team] == venue)
        {
            streak[team]++;
            if (streak[team] > 2)
                longStreakPenalty += (streak[team] - 2);
            return 1;
        }

        lastVenue[team] = venue;
        streak[team] = 1;
        return 0;
    }

    private static PairOrientationMetrics EvaluatePairOrientation(
        IReadOnlyList<bool?> lastWasHome,
        IReadOnlyList<int> sameVenueStreak,
        IReadOnlyList<int> homeCounts,
        IReadOnlyList<int> awayCounts,
        int min,
        int max,
        bool minIsHome)
    {
        var minMetrics = EvaluateTeamProjection(lastWasHome, sameVenueStreak, homeCounts, awayCounts, min, minIsHome);
        var maxMetrics = EvaluateTeamProjection(lastWasHome, sameVenueStreak, homeCounts, awayCounts, max, !minIsHome);
        return new PairOrientationMetrics(
            minMetrics.Repeated + maxMetrics.Repeated,
            Math.Max(minMetrics.ProjectedStreak, maxMetrics.ProjectedStreak),
            minMetrics.ProjectedAbsBalance + maxMetrics.ProjectedAbsBalance);
    }

    private static TeamProjectionMetrics EvaluateTeamProjection(
        IReadOnlyList<bool?> lastWasHome,
        IReadOnlyList<int> sameVenueStreak,
        IReadOnlyList<int> homeCounts,
        IReadOnlyList<int> awayCounts,
        int team,
        bool isHome)
    {
        var prev = lastWasHome[team];
        var repeated = prev.HasValue && prev.Value == isHome ? 1 : 0;
        var projectedStreak = repeated == 1 ? sameVenueStreak[team] + 1 : 1;
        var projectedHome = homeCounts[team] + (isHome ? 1 : 0);
        var projectedAway = awayCounts[team] + (isHome ? 0 : 1);
        var projectedAbsBalance = Math.Abs(projectedHome - projectedAway);
        return new TeamProjectionMetrics(repeated, projectedStreak, projectedAbsBalance);
    }

    private static bool IsMinHomeBetterForBalance(int min, int max, IReadOnlyList<int> homeCounts, IReadOnlyList<int> awayCounts)
    {
        var minHomeDiff = Math.Abs((homeCounts[min] + 1) - awayCounts[min]) +
                          Math.Abs(homeCounts[max] - (awayCounts[max] + 1));
        var maxHomeDiff = Math.Abs(homeCounts[min] - (awayCounts[min] + 1)) +
                          Math.Abs((homeCounts[max] + 1) - awayCounts[max]);
        if (minHomeDiff < maxHomeDiff) return true;
        if (maxHomeDiff < minHomeDiff) return false;

        if (homeCounts[min] < homeCounts[max]) return true;
        if (homeCounts[max] < homeCounts[min]) return false;
        return min <= max;
    }

    private static void UpdateTeamVenueTracking(
        int team,
        bool isHome,
        bool?[] lastWasHome,
        int[] sameVenueStreak,
        int[] homeCounts,
        int[] awayCounts)
    {
        var prev = lastWasHome[team];
        if (prev.HasValue && prev.Value == isHome)
            sameVenueStreak[team]++;
        else
            sameVenueStreak[team] = 1;

        lastWasHome[team] = isHome;
        if (isHome) homeCounts[team]++;
        else awayCounts[team]++;
    }

    private readonly record struct TeamProjectionMetrics(int Repeated, int ProjectedStreak, int ProjectedAbsBalance);
    private readonly record struct PairOrientationMetrics(int RepeatedCount, int MaxProjectedStreak, int BalancePenalty);
}
