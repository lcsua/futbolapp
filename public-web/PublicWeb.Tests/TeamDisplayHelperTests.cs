using PublicWeb.Helpers;

namespace PublicWeb.Tests;

public class TeamDisplayHelperTests
{
    [Theory]
    [InlineData("LA BARRA FC", "LA ", "BARRA")]
    [InlineData("LA BARRA FC", "LA", "BARRA")]
    [InlineData("LA UOCRA F.C", "LA ", "UOCRA")]
    [InlineData("LA UOCRA F.C.", null, "UOCRA")]
    [InlineData("ATL BALAK", "ATL", "BALAK")]
    [InlineData("DEF PAMPA BLANCA", "DEF", "PAMPA BLANCA")]
    [InlineData("CLUB ATLETICO RIVER", "", "RIVER")]
    [InlineData("LAS PIEDRAS", "LAS", "PIEDRAS")]
    [InlineData("EL TALENTO FC", null, "TALENTO")]
    [InlineData("Sportivo Luqueño", null, "Luqueño")]
    [InlineData("BARRA", null, "BARRA")]
    public void GetCompactName_KeepsDistinctiveWords(string name, string? shortName, string expected)
    {
        Assert.Equal(expected, TeamDisplayHelper.GetCompactName(name, shortName));
    }

    [Fact]
    public void GetCompactName_UsesCustomShortName_WhenItIsActuallyDistinct()
    {
        Assert.Equal("UOCRA", TeamDisplayHelper.GetCompactName("LA UOCRA F.C", "UOCRA"));
    }

    [Fact]
    public void GetCompactName_FallsBackToFullName_WhenOnlySkipTokensRemain()
    {
        Assert.Equal("CLUB ATLETICO", TeamDisplayHelper.GetCompactName("CLUB ATLETICO", null));
    }
}
