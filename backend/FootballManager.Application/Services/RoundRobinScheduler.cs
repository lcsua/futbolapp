using System;
using System.Collections.Generic;
using System.Linq;

namespace FootballManager.Application.Services;

/// <summary>
/// Deterministic round-robin scheduler. Fixed team at index 0, rotate others clockwise.
/// </summary>
public static class RoundRobinScheduler
{
    /// <summary>
    /// Generate round-robin pairings. Each inner list is a round of (home, away) pairs.
    /// Teams are identified by index 0..N-1. For N teams: N-1 rounds, N/2 matches per round.
    /// If isHomeAway: doubles the rounds (return leg - same pairings with home/away swapped).
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<(int Home, int Away)>> Generate(int teamCount, bool isHomeAway)
    {
        if (teamCount < 2)
            return Array.Empty<IReadOnlyList<(int, int)>>();

        // Ensure even: use BYE (phantom team) if odd
        var n = teamCount % 2 == 0 ? teamCount : teamCount + 1;
        var matchesPerRound = n / 2;
        var halfRounds = n - 1;

        var result = new List<IReadOnlyList<(int, int)>>();

        // Fixed team at 0, rotate 1..n-1 clockwise
        var ring = Enumerable.Range(1, n - 1).ToArray();

        for (var r = 0; r < halfRounds; r++)
        {
            var roundPairs = new List<(int, int)>(matchesPerRound);
            roundPairs.Add((0, ring[0]));
            for (var i = 1; i < matchesPerRound; i++)
            {
                var a = ring[i];
                var b = ring[ring.Length - i];
                roundPairs.Add((a, b));
            }
            result.Add(roundPairs);

            if (r < halfRounds - 1)
            {
                var last = ring[^1];
                Array.Copy(ring, 0, ring, 1, ring.Length - 1);
                ring[0] = last;
            }
        }

        if (isHomeAway)
        {
            foreach (var round in result.ToList())
            {
                var returnRound = round.Select(p => (p.Item2, p.Item1)).ToList();
                result.Add(returnRound);
            }
        }

        return result;
    }
}
