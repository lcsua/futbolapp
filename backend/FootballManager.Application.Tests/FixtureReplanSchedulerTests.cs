using FootballManager.Application.Services;
using static FootballManager.Application.Services.FixtureReplanScheduler;

namespace FootballManager.Application.Tests;

public class FixtureReplanSchedulerTests
{
    private static List<Guid> Teams(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    /// <summary>Played history: the first <paramref name="playedRounds"/> rounds of a circle round-robin.</summary>
    private static List<ExistingMatch> History(IReadOnlyList<Guid> teams, int playedRounds, int firstReplannedRound)
    {
        var rounds = RoundRobinScheduler.Generate(teams.Count, isHomeAway: false);
        var history = new List<ExistingMatch>();
        for (var r = 0; r < playedRounds; r++)
            foreach (var (h, a) in rounds[r])
                history.Add(new ExistingMatch(teams[h], teams[a], r - firstReplannedRound));
        return history;
    }

    private static (Guid, Guid) Key(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    private static void AssertNobodyPlaysTwicePerRound(IReadOnlyList<IReadOnlyList<PlannedMatch>> plan)
    {
        foreach (var round in plan)
        {
            var teams = round.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).ToList();
            Assert.Equal(teams.Count, teams.Distinct().Count());
        }
    }

    [Fact]
    public void Plan_TeamsAddedMidSeason_RecoverMissedMatchesAndEveryPairMeetsOnce()
    {
        var zoneA = Teams(6);
        var zoneB = Teams(6);
        var existing = History(zoneA, 3, 3).Concat(History(zoneB, 3, 3)).ToList();
        var newA = Guid.NewGuid();
        var newB = Guid.NewGuid();
        zoneA.Add(newA);
        zoneB.Add(newB);

        var plan = Plan(
            new[] { new Zone(Guid.NewGuid(), zoneA), new Zone(Guid.NewGuid(), zoneB) },
            existing,
            isHomeAway: false,
            fillByesWithInterzonal: true);

        AssertNobodyPlaysTwicePerRound(plan);

        foreach (var zone in new[] { zoneA, zoneB })
        {
            var meetings = existing.Select(m => Key(m.HomeTeamId, m.AwayTeamId))
                .Concat(plan.SelectMany(r => r).Where(m => !m.IsInterzonal).Select(m => Key(m.HomeTeamId, m.AwayTeamId)))
                .Where(k => zone.Contains(k.Item1) && zone.Contains(k.Item2))
                .GroupBy(k => k)
                .ToDictionary(g => g.Key, g => g.Count());

            Assert.Equal(zone.Count * (zone.Count - 1) / 2, meetings.Count);
            Assert.All(meetings.Values, c => Assert.Equal(1, c));
        }

        // New team needs 6 matches, so 6 rounds is the lower bound.
        Assert.Equal(6, plan.Count);
        Assert.All(plan.SelectMany(r => r).Where(m => m.IsInterzonal),
            m => Assert.NotEqual(zoneA.Contains(m.HomeTeamId), zoneA.Contains(m.AwayTeamId)));
    }

    [Fact]
    public void Plan_InterzonalFillsByes_NoRepeatedInterzonalPairs()
    {
        var zoneA = Teams(7);
        var zoneB = Teams(7);

        var plan = Plan(
            new[] { new Zone(Guid.NewGuid(), zoneA), new Zone(Guid.NewGuid(), zoneB) },
            Array.Empty<ExistingMatch>(),
            isHomeAway: false,
            fillByesWithInterzonal: true);

        AssertNobodyPlaysTwicePerRound(plan);
        Assert.Equal(7, plan.Count);
        Assert.All(plan, round => Assert.Equal(7, round.Count));

        var interzonalPairs = plan.SelectMany(r => r).Where(m => m.IsInterzonal)
            .Select(m => Key(m.HomeTeamId, m.AwayTeamId)).ToList();
        Assert.Equal(7, interzonalPairs.Count);
        Assert.Equal(interzonalPairs.Count, interzonalPairs.Distinct().Count());
    }

    [Fact]
    public void Plan_WithoutInterzonal_LeavesByes()
    {
        var zoneA = Teams(5);
        var zoneB = Teams(5);

        var plan = Plan(
            new[] { new Zone(Guid.NewGuid(), zoneA), new Zone(Guid.NewGuid(), zoneB) },
            Array.Empty<ExistingMatch>(),
            isHomeAway: false,
            fillByesWithInterzonal: false);

        Assert.DoesNotContain(plan.SelectMany(r => r), m => m.IsInterzonal);
        Assert.All(plan, round => Assert.Equal(4, round.Count));
    }

    [Fact]
    public void Plan_HomeAway_ReturnLegIsOppositeVenueOfPlayedFirstLeg()
    {
        var zone = Teams(4);
        var existing = new List<ExistingMatch>
        {
            new(zone[0], zone[1], -2),
            new(zone[2], zone[3], -2),
            new(zone[0], zone[2], -1),
            new(zone[1], zone[3], -1),
        };
        var newcomer = Guid.NewGuid();
        zone.Add(newcomer);

        var plan = Plan(new[] { new Zone(Guid.NewGuid(), zone) }, existing, isHomeAway: true, fillByesWithInterzonal: true);

        AssertNobodyPlaysTwicePerRound(plan);
        var all = existing.Select(m => (m.HomeTeamId, m.AwayTeamId))
            .Concat(plan.SelectMany(r => r).Select(m => (m.HomeTeamId, m.AwayTeamId)))
            .ToList();

        for (var i = 0; i < zone.Count; i++)
        {
            for (var j = i + 1; j < zone.Count; j++)
            {
                Assert.Contains((zone[i], zone[j]), all);
                Assert.Contains((zone[j], zone[i]), all);
                Assert.Equal(2, all.Count(m => Key(m.HomeTeamId, m.AwayTeamId) == Key(zone[i], zone[j])));
            }
        }
    }

    [Fact]
    public void Plan_LockedMatchInsideRange_BlocksThatTeamOnThatRound()
    {
        var zoneA = Teams(3);
        var zoneB = Teams(3);
        var outsider = Guid.NewGuid();
        var existing = new List<ExistingMatch> { new(zoneA[0], outsider, 0) };

        var plan = Plan(
            new[] { new Zone(Guid.NewGuid(), zoneA), new Zone(Guid.NewGuid(), zoneB) },
            existing,
            isHomeAway: false,
            fillByesWithInterzonal: true);

        Assert.DoesNotContain(plan[0], m => m.HomeTeamId == zoneA[0] || m.AwayTeamId == zoneA[0]);
    }
}
