using FootballManager.Application.Helpers;

namespace FootballManager.Application.Tests;

public class SlugGeneratorMatchTests
{
    [Fact]
    public void GenerateMatchSlug_UsesTeamNamesAndOptionalSeason()
    {
        Assert.Equal(
            "jueves-de-sometidos-vs-dep-ctj",
            SlugGenerator.GenerateMatchSlug("JUEVES DE SOMETIDOS", "DEP CTJ"));

        Assert.Equal(
            "jueves-de-sometidos-vs-dep-ctj-clausura-2026",
            SlugGenerator.GenerateMatchSlug("JUEVES DE SOMETIDOS", "DEP CTJ", "Clausura 2026"));
    }

    [Theory]
    [InlineData("jueves-de-sometidos-vs-dep-ctj", "jueves-de-sometidos", "dep-ctj")]
    [InlineData("jueves-de-sometidos-vs-dep-ctj-clausura-2026", "jueves-de-sometidos", "dep-ctj-clausura-2026")]
    [InlineData("JUEVES DE SOMETIDOS vs DEP CTJ", "jueves-de-sometidos", "dep-ctj")]
    public void TryParseMatchSlug_ReadsHomeAndRemainder(string input, string home, string remainder)
    {
        Assert.True(SlugGenerator.TryParseMatchSlug(input, out var homeSlug, out var awayAndSeason));
        Assert.Equal(home, homeSlug);
        Assert.Equal(remainder, awayAndSeason);
    }

    [Fact]
    public void TryParseMatchSlug_RejectsEmpty()
    {
        Assert.False(SlugGenerator.TryParseMatchSlug("solo-un-equipo", out _, out _));
        Assert.False(SlugGenerator.TryParseMatchSlug("", out _, out _));
    }
}
