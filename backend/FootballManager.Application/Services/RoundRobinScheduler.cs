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

        var result = new List<IReadOnlyList<(int Home, int Away)>>(unorderedRounds.Count);

        foreach (var round in unorderedRounds)
        {
            var oriented = new List<(int Home, int Away)>(round.Count);
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
                    // Independiente por pareja: prioriza cortar rachas y balancear local/visitante acumulado.
                    var minHomeScore = OrientationScore(lastWasHome, sameVenueStreak, homeCounts, awayCounts, min, isHome: true) +
                                       OrientationScore(lastWasHome, sameVenueStreak, homeCounts, awayCounts, max, isHome: false);
                    var maxHomeScore = OrientationScore(lastWasHome, sameVenueStreak, homeCounts, awayCounts, min, isHome: false) +
                                       OrientationScore(lastWasHome, sameVenueStreak, homeCounts, awayCounts, max, isHome: true);

                    if (minHomeScore > maxHomeScore)
                    {
                        home = min;
                        away = max;
                    }
                    else if (maxHomeScore > minHomeScore)
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
                oriented.Add((home, away));
            }

            result.Add(oriented);
        }

        return result;
    }

    /// <summary>Higher score = better orientation considering alternation streaks and current balance.</summary>
    private static int OrientationScore(
        IReadOnlyList<bool?> lastWasHome,
        IReadOnlyList<int> sameVenueStreak,
        IReadOnlyList<int> homeCounts,
        IReadOnlyList<int> awayCounts,
        int team,
        bool isHome)
    {
        var prev = lastWasHome[team];
        var alternationScore = prev switch
        {
            null => 2,
            true when isHome => -2 - sameVenueStreak[team],
            false when !isHome => -2 - sameVenueStreak[team],
            _ => 4 + sameVenueStreak[team]
        };

        var balance = homeCounts[team] - awayCounts[team];
        var balanceScore = isHome ? -balance : balance;
        return alternationScore + balanceScore;
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
}
