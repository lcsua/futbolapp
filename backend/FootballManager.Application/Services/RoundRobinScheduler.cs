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
                    // Independiente por pareja: maximiza alternancia respecto a la fecha anterior
                    var minHomeScore = AlternationScore(lastWasHome, min, isHome: true) +
                                       AlternationScore(lastWasHome, max, isHome: false);
                    var maxHomeScore = AlternationScore(lastWasHome, min, isHome: false) +
                                       AlternationScore(lastWasHome, max, isHome: true);

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
                        // Empate en satisfacción: índice menor de local (determinístico)
                        home = min;
                        away = max;
                    }

                    firstLegMinWasHome[(min, max)] = home == min;
                }

                lastWasHome[home] = true;
                lastWasHome[away] = false;
                oriented.Add((home, away));
            }

            result.Add(oriented);
        }

        return result;
    }

    /// <summary>Higher score = better match for alternating home/away after the previous round.</summary>
    private static int AlternationScore(IReadOnlyList<bool?> lastWasHome, int team, bool isHome)
    {
        var prev = lastWasHome[team];
        if (prev == null)
            return 1;
        if (prev.Value)
            return isHome ? 0 : 2;
        return isHome ? 2 : 0;
    }
}
