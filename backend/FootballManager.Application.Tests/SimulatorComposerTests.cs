using FootballManager.Api.Models.Public;
using FootballManager.Api.Services.Public;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Application.Tests;

public class SimulatorComposerTests
{
    private static readonly Guid DivisionSeasonA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    private static readonly Guid DivisionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    private static readonly Guid DivisionSeasonB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    private static readonly Guid DivisionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    private static readonly Guid Atlas = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Boca = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid Central = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Fact]
    public void Pending_IncludesPostponedAndSuspended_AndSkipsFinishedCancelledAndByes()
    {
        var model = Compose(
            roster: new[] { Atlas, Boca, Central },
            fixtures: new[]
            {
                Fixture(1, "COMPLETED", true, Atlas, Boca),
                Fixture(2, "POSTPONED", false, Atlas, Boca),
                Fixture(2, "SUSPENDED", false, Central, Boca),
                Fixture(3, "CANCELLED", false, Atlas, Boca),
                Fixture(3, "SCHEDULED", false, Central, Boca)
            },
            standings: new[] { Standing(Atlas, "Atlas", 1, 3) });

        var division = Assert.Single(model.Divisions);
        Assert.Equal(3, division.PendingCount);
        Assert.Equal(new[] { 2, 3 }, division.Rounds.Select(round => round.Round));
        Assert.Contains(division.Rounds[0].Matches, match => match.Status == "POSTPONED" && match.StatusLabel == "Aplazado");
        Assert.Contains(division.Rounds[0].Matches, match => match.Status == "SUSPENDED");
        Assert.DoesNotContain(division.Rounds.SelectMany(round => round.Matches), match => match.Status is "COMPLETED" or "CANCELLED");
        Assert.Single(division.Official);
        Assert.Equal(3, division.Official[0].Points);
        Assert.Equal("Atlas", division.Official[0].Team.Name);

        var round3 = division.Rounds.Single(round => round.Round == 3);
        Assert.DoesNotContain(round3.Byes, bye => bye.Team.Id == Atlas || bye.Team.Id == Boca || bye.Team.Id == Central);
    }

    [Fact]
    public void Bye_IsShownOnAPendingRoundWithoutAFixture()
    {
        var model = Compose(
            roster: new[] { Atlas, Boca, Central },
            fixtures: new[] { Fixture(4, "SCHEDULED", false, Atlas, Boca) },
            standings: Array.Empty<TeamStandingDto>());

        var bye = Assert.Single(Assert.Single(model.Divisions).Rounds[0].Byes);
        Assert.Equal(Central, bye.Team.Id);
        Assert.Equal("Central", bye.Team.Name);
    }

    [Fact]
    public void FinishedDivision_HasNoPendingRounds()
    {
        var model = Compose(
            roster: new[] { Atlas, Boca },
            fixtures: new[] { Fixture(1, "COMPLETED", true, Atlas, Boca) },
            standings: new[] { Standing(Atlas, "Atlas", 1, 3), Standing(Boca, "Boca", 2, 0) });

        var division = Assert.Single(model.Divisions);
        Assert.Equal(0, division.PendingCount);
        Assert.Empty(division.Rounds);
        Assert.Null(model.Warning);
    }

    [Fact]
    public void PlayedResult_IsNotEditable_AndWarns()
    {
        var model = Compose(
            roster: new[] { Atlas, Boca },
            fixtures: new[] { Fixture(1, "PLAYED", true, Atlas, Boca) },
            standings: Array.Empty<TeamStandingDto>());

        Assert.Equal(0, Assert.Single(model.Divisions).PendingCount);
        Assert.Contains("no suma", model.Warning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DivisionFilter_DoesNotMixFixtures()
    {
        var model = SimulatorComposer.Compose(
            "Clausura 2026",
            "clausura-2026",
            "b",
            new[]
            {
                new SimulatorDivisionInput(DivisionSeasonA, DivisionA, "A", new[] { Atlas }),
                new SimulatorDivisionInput(DivisionSeasonB, DivisionB, "B", new[] { Boca })
            },
            Array.Empty<DivisionStandingsDto>(),
            new[]
            {
                Fixture(1, "SCHEDULED", false, Atlas, Boca, DivisionSeasonA, DivisionSeasonA, DivisionSeasonB)
            },
            Teams());

        var division = Assert.Single(model.Divisions);
        Assert.Equal("b", division.DivisionSlug);
        var match = Assert.Single(Assert.Single(division.Rounds).Matches);
        Assert.False(match.HomeCounts);
        Assert.True(match.AwayCounts);
    }

    private static SimulatorPublicDto Compose(
        IReadOnlyList<Guid> roster,
        IReadOnlyList<SimulatorFixtureInput> fixtures,
        IReadOnlyList<TeamStandingDto> standings) =>
        SimulatorComposer.Compose(
            "Clausura 2026",
            "clausura-2026",
            null,
            new[] { new SimulatorDivisionInput(DivisionSeasonA, DivisionA, "A", roster) },
            new[] { new DivisionStandingsDto(DivisionA, "A", standings) },
            fixtures,
            Teams());

    private static SimulatorFixtureInput Fixture(
        int round,
        string status,
        bool hasResult,
        Guid home,
        Guid away,
        Guid? fixtureDivision = null,
        Guid? homeDivision = null,
        Guid? awayDivision = null) =>
        new(
            Guid.NewGuid(),
            round,
            new DateTime(2026, 11, 20, 16, 0, 0),
            status,
            hasResult,
            fixtureDivision ?? DivisionSeasonA,
            home,
            homeDivision ?? DivisionSeasonA,
            away,
            awayDivision ?? DivisionSeasonA);

    private static TeamStandingDto Standing(Guid id, string name, int position, int points) =>
        new(position, id, name, points, points > 0 ? 1 : 0, points > 0 ? 1 : 0, 0, points > 0 ? 0 : 1, points > 0 ? 1 : 0, points > 0 ? 0 : 1, points > 0 ? 1 : -1);

    private static Dictionary<Guid, TeamPublicDto> Teams() => new()
    {
        [Atlas] = new TeamPublicDto { Id = Atlas, Name = "Atlas", Slug = "atlas", LogoUrl = "/atlas.png" },
        [Boca] = new TeamPublicDto { Id = Boca, Name = "Boca", Slug = "boca", LogoUrl = "/boca.png" },
        [Central] = new TeamPublicDto { Id = Central, Name = "Central", Slug = "central", LogoUrl = "/central.png" }
    };
}
