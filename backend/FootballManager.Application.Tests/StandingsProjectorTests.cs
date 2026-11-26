using FootballManager.Application.Standings;

namespace FootballManager.Application.Tests;

public class StandingsProjectorTests
{
    private static readonly Guid DivisionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid DivisionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid TeamA = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid TeamB = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid TeamC = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid TeamD = Guid.Parse("10000000-0000-0000-0000-000000000004");

    [Fact]
    public void WinDrawAndLoss_UpdatePointsGoalsAndDifference()
    {
        var projection = StandingsProjector.FromResults(new[]
        {
            Match(TeamA, "Atlas", TeamB, "Boca", 2, 0),
            Match(TeamA, "Atlas", TeamC, "Central", 1, 1),
            Match(TeamC, "Central", TeamB, "Boca", 0, 3)
        });

        var table = Only(projection);
        var atlas = Row(table, TeamA);
        var boca = Row(table, TeamB);
        var central = Row(table, TeamC);

        Assert.Equal(4, atlas.Points);
        Assert.Equal(2, atlas.Played);
        Assert.Equal(1, atlas.Wins);
        Assert.Equal(1, atlas.Draws);
        Assert.Equal(0, atlas.Losses);
        Assert.Equal(3, atlas.GoalsFor);
        Assert.Equal(1, atlas.GoalsAgainst);
        Assert.Equal(2, atlas.GoalDifference);

        Assert.Equal(3, boca.Points);
        Assert.Equal(1, boca.Wins);
        Assert.Equal(1, boca.Losses);
        Assert.Equal(3, boca.GoalsFor);
        Assert.Equal(2, boca.GoalsAgainst);

        Assert.Equal(1, central.Points);
        Assert.Equal(1, central.Draws);
        Assert.Equal(1, central.Losses);

        Assert.Equal(new[] { TeamA, TeamB, TeamC }, table.Teams.Select(team => team.TeamId));
    }

    [Fact]
    public void TieBreak_UsesGoalDifferenceThenGoalsFor()
    {
        var projection = StandingsProjector.FromResults(new[]
        {
            Match(TeamA, "Atlas", TeamD, "Don Bosco", 1, 0),
            Match(TeamB, "Boca", TeamD, "Don Bosco", 3, 2),
            Match(TeamC, "Central", TeamD, "Don Bosco", 4, 0)
        });

        var ids = Only(projection).Teams.Select(team => team.TeamId).ToList();
        Assert.Equal(TeamC, ids[0]);
        Assert.Equal(TeamB, ids[1]);
        Assert.Equal(TeamA, ids[2]);
    }

    [Fact]
    public void FullTie_KeepsTheSideThatAppearedFirst()
    {
        var projection = StandingsProjector.FromResults(new[]
        {
            Match(TeamA, "Atlas", TeamB, "Boca", 1, 1)
        });

        Assert.Equal(new[] { TeamA, TeamB }, Only(projection).Teams.Select(team => team.TeamId));
    }

    [Fact]
    public void Sanction_DropsThePenalisedTeam()
    {
        var projection = StandingsProjector.FromResults(
            new[]
            {
                Match(TeamA, "Atlas", TeamC, "Central", 1, 0),
                Match(TeamB, "Boca", TeamD, "Don Bosco", 1, 0)
            },
            new[]
            {
                new PointAdjustment(TeamA, "Atlas", DivisionA, "A", -3)
            });

        var table = Only(projection);
        Assert.Equal(0, Row(table, TeamA).Points);
        Assert.Equal(TeamB, table.Teams[0].TeamId);
        Assert.Equal(TeamA, table.Teams[1].TeamId);
    }

    [Fact]
    public void Interzonal_CountsEachSideInItsOwnDivision()
    {
        var projection = StandingsProjector.FromResults(new[]
        {
            new CountedResult(
                new StandingSide(TeamA, "Atlas", DivisionA, "A"),
                new StandingSide(TeamB, "Boca", DivisionB, "B"),
                2,
                1)
        });

        Assert.Equal(2, projection.Divisions.Count);
        var zoneA = projection.Divisions.Single(d => d.DivisionId == DivisionA);
        var zoneB = projection.Divisions.Single(d => d.DivisionId == DivisionB);
        Assert.Equal(3, zoneA.Teams.Single().Points);
        Assert.Equal(1, zoneA.Teams.Single().Wins);
        Assert.Equal(0, zoneB.Teams.Single().Points);
        Assert.Equal(1, zoneB.Teams.Single().Losses);
    }

    [Fact]
    public void BaselineWithoutScores_MatchesTheOfficialOrder()
    {
        var baseline = new[]
        {
            Base(TeamB, "Boca", 1, points: 4, gf: 2, ga: 2),
            Base(TeamA, "Atlas", 2, points: 4, gf: 2, ga: 2)
        };

        var projection = StandingsProjector.FromBaseline(DivisionA, "A", baseline, Array.Empty<SimulatedScore>());

        Assert.Equal(0, projection.SimulatedMatches);
        Assert.Equal(new[] { TeamB, TeamA }, projection.Divisions[0].Teams.Select(team => team.TeamId));
        Assert.Equal(0, projection.Divisions[0].Teams[0].PointsDelta);
    }

    [Fact]
    public void Baseline_AppliesOnlyCompleteNonNegativeScores()
    {
        var baseline = new[] { Base(TeamA, "Atlas", 1, points: 3, played: 1, wins: 1, gf: 1, ga: 0) };
        var scores = new[]
        {
            Score(TeamA, TeamB, null, 1),
            Score(TeamA, TeamB, 2, null),
            Score(TeamA, TeamB, -1, 0),
            Score(TeamA, TeamB, 1, 1)
        };

        var projection = StandingsProjector.FromBaseline(DivisionA, "A", baseline, scores);
        var atlas = projection.Divisions[0].Teams.Single(team => team.TeamId == TeamA);

        Assert.Equal(1, projection.SimulatedMatches);
        Assert.Equal(4, atlas.Points);
        Assert.Equal(2, atlas.Played);
        Assert.Equal(1, atlas.Draws);
        Assert.Equal(2, atlas.GoalsFor);
        Assert.Equal(1, atlas.GoalsAgainst);
        Assert.Equal(1, atlas.PointsDelta);
        Assert.Contains(projection.Divisions[0].Teams, team => team.TeamId == TeamB && team.OfficialPosition == 0);
    }

    [Fact]
    public void Baseline_PartialRoundDoesNotInventTheMissingMatch()
    {
        var baseline = new[]
        {
            Base(TeamA, "Atlas", 1, points: 0),
            Base(TeamB, "Boca", 2, points: 0)
        };
        var pending = new[]
        {
            Score(TeamA, TeamC, 2, 0),
            Score(TeamB, TeamD, null, null)
        };

        var projection = StandingsProjector.FromBaseline(DivisionA, "A", baseline, pending);
        Assert.Equal(1, projection.SimulatedMatches);
        Assert.Equal(3, Row(projection.Divisions[0], TeamA).Points);
        Assert.Equal(0, Row(projection.Divisions[0], TeamB).Points);
        Assert.Equal(0, Row(projection.Divisions[0], TeamB).Played);
    }

    [Fact]
    public void UnsupportedTieBreak_DoesNotRank()
    {
        var rules = new StandingsRules(3, 1, 0, new[] { "points", "headToHead" });
        var projection = StandingsProjector.FromBaseline(
            DivisionA,
            "A",
            new[] { Base(TeamA, "Atlas", 1, points: 3) },
            new[] { Score(TeamA, TeamB, 1, 0) },
            rules: rules);

        Assert.False(projection.Supported);
        Assert.Empty(projection.Divisions);
        Assert.Contains("desempate", projection.Limitation, StringComparison.OrdinalIgnoreCase);
    }

    private static CountedResult Match(Guid home, string homeName, Guid away, string awayName, int homeGoals, int awayGoals) =>
        new(
            new StandingSide(home, homeName, DivisionA, "A"),
            new StandingSide(away, awayName, DivisionA, "A"),
            homeGoals,
            awayGoals);

    private static BaselineTeam Base(
        Guid id,
        string name,
        int position,
        int points = 0,
        int played = 0,
        int wins = 0,
        int draws = 0,
        int losses = 0,
        int gf = 0,
        int ga = 0) =>
        new(id, name, position, played, wins, draws, losses, gf, ga, points);

    private static SimulatedScore Score(Guid home, Guid away, int? homeGoals, int? awayGoals) =>
        new(home, "Local", true, away, "Visita", true, homeGoals, awayGoals);

    private static ProjectedDivision Only(Projection projection)
    {
        Assert.True(projection.Supported);
        return Assert.Single(projection.Divisions);
    }

    private static ProjectedTeam Row(ProjectedDivision division, Guid teamId) =>
        division.Teams.Single(team => team.TeamId == teamId);
}
