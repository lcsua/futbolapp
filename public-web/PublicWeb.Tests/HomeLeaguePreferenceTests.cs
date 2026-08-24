using PublicWeb.Helpers;

namespace PublicWeb.Tests;

public class HomeLeaguePreferenceTests
{
    [Theory]
    [InlineData("veteranos-de-perico", true)]
    [InlineData("argentina/primera-division", true)]
    [InlineData("default-team", true)]
    [InlineData("/ligas/veteranos", false)]
    [InlineData("veteranos de perico", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidPath_AcceptsLeagueSlugsOnly(string? value, bool expected)
    {
        Assert.Equal(expected, HomeLeaguePreference.IsValidPath(value));
    }

    [Fact]
    public void Resolve_PrefersPinnedLeagueOverLastVisited()
    {
        Assert.Equal(
            "fija",
            HomeLeaguePreference.Resolve("fija", "ultima"));
        Assert.Equal(
            "ultima",
            HomeLeaguePreference.Resolve(null, "ultima"));
        Assert.Null(HomeLeaguePreference.Resolve("../x", "not valid"));
    }

    [Fact]
    public void ToPublicUrl_UsesApexLeaguePath()
    {
        Assert.Equal("/ligas/veteranos-de-perico", HomeLeaguePreference.ToPublicUrl("veteranos-de-perico"));
    }
}
