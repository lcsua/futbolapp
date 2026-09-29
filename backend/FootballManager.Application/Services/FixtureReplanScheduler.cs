using System;
using System.Collections.Generic;
using System.Linq;

namespace FootballManager.Application.Services;

/// <summary>
/// Re-plans the pending part of a (possibly zoned) round-robin from a given matchday on.
/// Every pair of teams inside a zone must meet once (twice with home/away); meetings that already
/// happened (or are locked) are kept and only the missing ones are packed into new rounds, so teams
/// added mid-season recover the matchdays they missed. Optionally, teams left without a match in a
/// round are paired with free teams of another zone (interzonal) so nobody rests.
/// </summary>
public static class FixtureReplanScheduler
{
    public sealed record Zone(Guid ZoneId, IReadOnlyList<Guid> TeamIds);

    /// <param name="RoundIndex">Relative to the first re-planned round: negative = before it (history);
    /// 0 or more = a locked match inside the re-planned range (its teams are busy that round).</param>
    public sealed record ExistingMatch(Guid HomeTeamId, Guid AwayTeamId, int RoundIndex);

    public sealed record PlannedMatch(Guid HomeTeamId, Guid AwayTeamId, bool IsInterzonal);

    private sealed class PendingEdge
    {
        public int Id { get; init; }
        public Guid A { get; init; }
        public Guid B { get; init; }
        public Guid? ForcedHome { get; init; }
        public int? ReturnOfId { get; init; }
        public int? FixedRound { get; init; }
    }

    public static IReadOnlyList<IReadOnlyList<PlannedMatch>> Plan(
        IReadOnlyList<Zone> zones,
        IReadOnlyList<ExistingMatch> existing,
        bool isHomeAway,
        bool fillByesWithInterzonal,
        int attempts = 60)
    {
        var zoneOf = new Dictionary<Guid, Guid>();
        foreach (var zone in zones)
            foreach (var team in zone.TeamIds)
                zoneOf[team] = zone.ZoneId;

        var busy = existing
            .Where(m => m.RoundIndex >= 0)
            .SelectMany(m => new[] { (m.HomeTeamId, m.RoundIndex), (m.AwayTeamId, m.RoundIndex) })
            .ToHashSet();

        var edges = BuildPendingEdges(zones, existing, zoneOf, isHomeAway, busy);

        List<List<PendingEdge>>? best = null;
        for (var attempt = 0; attempt < Math.Max(1, attempts); attempt++)
        {
            var packed = Pack(edges, busy, new Random(attempt));
            if (packed == null) continue;
            if (best == null || packed.Count < best.Count)
                best = packed;
        }

        if (best == null)
            throw new InvalidOperationException("Could not pack the pending matches into rounds.");

        var lastBusyRound = busy.Count == 0 ? -1 : busy.Max(b => b.Item2);
        while (best.Count <= lastBusyRound)
            best.Add(new List<PendingEdge>());

        var interzonal = fillByesWithInterzonal && zones.Count > 1
            ? PairFreeTeamsAcrossZones(best, zones, existing, zoneOf, busy)
            : best.Select(_ => new List<(Guid, Guid)>()).ToList();

        return Orient(best, interzonal, existing);
    }

    private static List<PendingEdge> BuildPendingEdges(
        IReadOnlyList<Zone> zones,
        IReadOnlyList<ExistingMatch> existing,
        IReadOnlyDictionary<Guid, Guid> zoneOf,
        bool isHomeAway,
        HashSet<(Guid, int)> busy)
    {
        var legsRequired = isHomeAway ? 2 : 1;
        var homesByPair = new Dictionary<(Guid, Guid), List<Guid>>();
        foreach (var m in existing)
        {
            if (!zoneOf.TryGetValue(m.HomeTeamId, out var zh) || !zoneOf.TryGetValue(m.AwayTeamId, out var za) || zh != za)
                continue;
            var key = PairKey(m.HomeTeamId, m.AwayTeamId);
            if (!homesByPair.TryGetValue(key, out var homes))
                homesByPair[key] = homes = new List<Guid>();
            homes.Add(m.HomeTeamId);
        }

        var edges = new List<PendingEdge>();
        var nextId = 0;

        foreach (var zone in zones)
        {
            var teams = zone.TeamIds.OrderBy(t => t).ToList();
            var zoneIsFresh = teams.All(t => !busy.Any(b => b.Item1 == t))
                && !homesByPair.Keys.Any(k => zoneOf[k.Item1] == zone.ZoneId);

            if (zoneIsFresh && teams.Count >= 2)
            {
                var rounds = RoundRobinScheduler.Generate(teams.Count, isHomeAway);
                for (var r = 0; r < rounds.Count; r++)
                {
                    foreach (var (home, away) in rounds[r])
                    {
                        edges.Add(new PendingEdge
                        {
                            Id = nextId++,
                            A = teams[home],
                            B = teams[away],
                            ForcedHome = teams[home],
                            FixedRound = r,
                        });
                    }
                }
                continue;
            }

            for (var i = 0; i < teams.Count; i++)
            {
                for (var j = i + 1; j < teams.Count; j++)
                {
                    var a = teams[i];
                    var b = teams[j];
                    var homes = homesByPair.GetValueOrDefault(PairKey(a, b)) ?? new List<Guid>();
                    var remaining = legsRequired - homes.Count;
                    if (remaining <= 0) continue;

                    if (remaining == 1 && isHomeAway)
                    {
                        edges.Add(new PendingEdge { Id = nextId++, A = a, B = b, ForcedHome = homes[0] == a ? b : a });
                    }
                    else if (remaining == 2)
                    {
                        var first = new PendingEdge { Id = nextId++, A = a, B = b };
                        edges.Add(first);
                        edges.Add(new PendingEdge { Id = nextId++, A = a, B = b, ReturnOfId = first.Id });
                    }
                    else
                    {
                        edges.Add(new PendingEdge { Id = nextId++, A = a, B = b });
                    }
                }
            }
        }

        return edges;
    }

    private static List<List<PendingEdge>>? Pack(
        IReadOnlyList<PendingEdge> edges,
        HashSet<(Guid, int)> busy,
        Random random)
    {
        var rounds = new List<List<PendingEdge>>();
        var roundOf = new Dictionary<int, int>();
        var unscheduled = new List<PendingEdge>();

        foreach (var e in edges)
        {
            if (e.FixedRound is int fixedRound)
            {
                while (rounds.Count <= fixedRound) rounds.Add(new List<PendingEdge>());
                rounds[fixedRound].Add(e);
                roundOf[e.Id] = fixedRound;
            }
            else
            {
                unscheduled.Add(e);
            }
        }

        var maxRounds = edges.Count + (busy.Count == 0 ? 0 : busy.Max(b => b.Item2) + 1) + 4;
        var round = 0;
        while (unscheduled.Count > 0)
        {
            if (round > maxRounds) return null;
            while (rounds.Count <= round) rounds.Add(new List<PendingEdge>());

            var used = rounds[round].SelectMany(e => new[] { e.A, e.B }).ToHashSet();
            var degree = new Dictionary<Guid, int>();
            foreach (var e in unscheduled)
            {
                degree[e.A] = degree.GetValueOrDefault(e.A) + 1;
                degree[e.B] = degree.GetValueOrDefault(e.B) + 1;
            }

            var candidates = unscheduled
                .Where(e => e.ReturnOfId is not int firstId
                            || (roundOf.TryGetValue(firstId, out var firstRound) && firstRound < round - 1))
                .Select(e => (Edge: e, Tie: random.Next()))
                .OrderByDescending(x => Math.Max(degree[x.Edge.A], degree[x.Edge.B]))
                .ThenByDescending(x => degree[x.Edge.A] + degree[x.Edge.B])
                .ThenBy(x => x.Tie)
                .Select(x => x.Edge)
                .ToList();

            foreach (var e in candidates)
            {
                if (used.Contains(e.A) || used.Contains(e.B)) continue;
                if (busy.Contains((e.A, round)) || busy.Contains((e.B, round))) continue;
                rounds[round].Add(e);
                roundOf[e.Id] = round;
                used.Add(e.A);
                used.Add(e.B);
            }

            unscheduled.RemoveAll(e => roundOf.ContainsKey(e.Id));
            round++;
        }

        while (rounds.Count > 0 && rounds[^1].Count == 0)
            rounds.RemoveAt(rounds.Count - 1);

        return rounds;
    }

    private static List<List<(Guid, Guid)>> PairFreeTeamsAcrossZones(
        IReadOnlyList<List<PendingEdge>> rounds,
        IReadOnlyList<Zone> zones,
        IReadOnlyList<ExistingMatch> existing,
        IReadOnlyDictionary<Guid, Guid> zoneOf,
        HashSet<(Guid, int)> busy)
    {
        var meetings = new Dictionary<(Guid, Guid), int>();
        var interzonalCount = new Dictionary<Guid, int>();
        foreach (var m in existing)
        {
            if (!zoneOf.TryGetValue(m.HomeTeamId, out var zh) || !zoneOf.TryGetValue(m.AwayTeamId, out var za) || zh == za)
                continue;
            var key = PairKey(m.HomeTeamId, m.AwayTeamId);
            meetings[key] = meetings.GetValueOrDefault(key) + 1;
            interzonalCount[m.HomeTeamId] = interzonalCount.GetValueOrDefault(m.HomeTeamId) + 1;
            interzonalCount[m.AwayTeamId] = interzonalCount.GetValueOrDefault(m.AwayTeamId) + 1;
        }

        var allTeams = zones.SelectMany(z => z.TeamIds).OrderBy(t => t).ToList();
        var result = new List<List<(Guid, Guid)>>();

        for (var r = 0; r < rounds.Count; r++)
        {
            var playing = rounds[r].SelectMany(e => new[] { e.A, e.B }).ToHashSet();
            var free = allTeams
                .Where(t => !playing.Contains(t) && !busy.Contains((t, r)))
                .ToList();

            var pairs = new List<(Guid, Guid)>();
            while (free.Count > 1)
            {
                var team = free
                    .OrderByDescending(t => free.Count(o => zoneOf[o] == zoneOf[t]))
                    .ThenBy(t => interzonalCount.GetValueOrDefault(t))
                    .First();

                var partner = free
                    .Where(p => zoneOf[p] != zoneOf[team])
                    .OrderBy(p => meetings.GetValueOrDefault(PairKey(team, p)))
                    .ThenBy(p => interzonalCount.GetValueOrDefault(p))
                    .Select(p => (Guid?)p)
                    .FirstOrDefault();

                if (partner is not Guid other)
                {
                    free.Remove(team);
                    continue;
                }

                pairs.Add((team, other));
                var key = PairKey(team, other);
                meetings[key] = meetings.GetValueOrDefault(key) + 1;
                interzonalCount[team] = interzonalCount.GetValueOrDefault(team) + 1;
                interzonalCount[other] = interzonalCount.GetValueOrDefault(other) + 1;
                free.Remove(team);
                free.Remove(other);
            }

            result.Add(pairs);
        }

        return result;
    }

    private static IReadOnlyList<IReadOnlyList<PlannedMatch>> Orient(
        IReadOnlyList<List<PendingEdge>> rounds,
        IReadOnlyList<List<(Guid, Guid)>> interzonal,
        IReadOnlyList<ExistingMatch> existing)
    {
        var lastWasHome = new Dictionary<Guid, bool>();
        var balance = new Dictionary<Guid, int>();

        void Track(Guid home, Guid away)
        {
            lastWasHome[home] = true;
            lastWasHome[away] = false;
            balance[home] = balance.GetValueOrDefault(home) + 1;
            balance[away] = balance.GetValueOrDefault(away) - 1;
        }

        foreach (var m in existing.OrderBy(m => m.RoundIndex))
            Track(m.HomeTeamId, m.AwayTeamId);

        (Guid Home, Guid Away) Choose(Guid a, Guid b)
        {
            int Repeats(Guid home, Guid away) =>
                (lastWasHome.TryGetValue(home, out var h) && h ? 1 : 0) +
                (lastWasHome.TryGetValue(away, out var w) && !w ? 1 : 0);

            var aHomeRepeats = Repeats(a, b);
            var bHomeRepeats = Repeats(b, a);
            if (aHomeRepeats != bHomeRepeats)
                return aHomeRepeats < bHomeRepeats ? (a, b) : (b, a);

            return balance.GetValueOrDefault(a) <= balance.GetValueOrDefault(b) ? (a, b) : (b, a);
        }

        var homeOfEdge = new Dictionary<int, Guid>();
        var result = new List<IReadOnlyList<PlannedMatch>>();

        for (var r = 0; r < rounds.Count; r++)
        {
            var planned = new List<PlannedMatch>();
            foreach (var e in rounds[r])
            {
                (Guid Home, Guid Away) pick;
                if (e.ForcedHome is Guid forced)
                    pick = forced == e.A ? (e.A, e.B) : (e.B, e.A);
                else if (e.ReturnOfId is int firstId && homeOfEdge.TryGetValue(firstId, out var firstHome))
                    pick = firstHome == e.A ? (e.B, e.A) : (e.A, e.B);
                else
                    pick = Choose(e.A, e.B);

                homeOfEdge[e.Id] = pick.Home;
                planned.Add(new PlannedMatch(pick.Home, pick.Away, IsInterzonal: false));
            }

            foreach (var (a, b) in interzonal[r])
            {
                var pick = Choose(a, b);
                planned.Add(new PlannedMatch(pick.Home, pick.Away, IsInterzonal: true));
            }

            foreach (var m in planned)
                Track(m.HomeTeamId, m.AwayTeamId);

            result.Add(planned);
        }

        return result;
    }

    private static (Guid, Guid) PairKey(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);
}
