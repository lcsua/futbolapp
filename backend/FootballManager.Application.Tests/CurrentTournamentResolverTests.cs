using FootballManager.Application.ProfessionalFootball;

namespace FootballManager.Application.Tests;

public class CurrentTournamentResolverTests
{
    private static IReadOnlyList<SeasonInfo> Season2026() =>
        new[]
        {
            new SeasonInfo(2026, new[]
            {
                new SeasonTypeInfo("1", "Torneo Apertura",
                    new DateTimeOffset(2026, 1, 1, 5, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 7, 3, 59, 0, TimeSpan.Zero),
                    true),
                new SeasonTypeInfo("2", "Apertura - Round of 16",
                    new DateTimeOffset(2026, 5, 7, 4, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 5, 12, 3, 59, 0, TimeSpan.Zero),
                    false),
                new SeasonTypeInfo("6", "Torneo Clausura",
                    new DateTimeOffset(2026, 7, 1, 4, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 11, 9, 4, 59, 0, TimeSpan.Zero),
                    true),
            }),
        };

    [Fact]
    public void Resolve_August2026_SelectsClausuraType6()
    {
        var now = new DateTimeOffset(2026, 8, 7, 15, 0, 0, TimeSpan.Zero);
        var result = CurrentTournamentResolver.Resolve(Season2026(), now);

        Assert.NotNull(result);
        Assert.Equal(2026, result!.SeasonYear);
        Assert.Equal("6", result.SeasonTypeId);
        Assert.Equal("Torneo Clausura", result.Name);
    }

    [Fact]
    public void Resolve_DuringApertura_SelectsType1()
    {
        var now = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var result = CurrentTournamentResolver.Resolve(Season2026(), now);

        Assert.NotNull(result);
        Assert.Equal("1", result!.SeasonTypeId);
        Assert.Equal("Torneo Apertura", result.Name);
    }

    [Fact]
    public void ResolveForStandings_DuringPlayoffs_FallsBackToApertura()
    {
        var now = new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);
        var result = CurrentTournamentResolver.ResolveForStandings(Season2026(), now);

        Assert.NotNull(result);
        Assert.Equal("1", result!.SeasonTypeId);
        Assert.Equal("Torneo Apertura", result.Name);
    }
}
