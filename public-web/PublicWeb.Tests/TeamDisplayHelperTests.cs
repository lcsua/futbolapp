using PublicWeb.Helpers;
using PublicWeb.Models.Public;

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

    [Fact]
    public void FormatUpcomingShareText_IncludesTeamsDateTimeVenueAndLeague()
    {
        var match = UpcomingMatch();

        var text = TeamDisplayHelper.FormatUpcomingShareText(match, "Liga de Veteranos de Perico");

        Assert.Equal(
            "Próximo partido\nDEP ELUNEY POPULAR vs JUEVES DE SOMETIDOS\n29 AGO · 13:10 · Cancha F\nLiga de Veteranos de Perico",
            text);
    }

    [Fact]
    public void FormatUpcomingOgTitle_NamesBothTeams()
    {
        Assert.Equal(
            "Próximo partido: DEP ELUNEY POPULAR vs JUEVES DE SOMETIDOS",
            TeamDisplayHelper.FormatUpcomingOgTitle(UpcomingMatch()));
    }

    [Fact]
    public void FormatUpcomingOgDescription_JoinsDetailsAndLeague()
    {
        Assert.Equal(
            "29 AGO · 13:10 · Cancha F · Liga de Veteranos de Perico",
            TeamDisplayHelper.FormatUpcomingOgDescription(UpcomingMatch(), "Liga de Veteranos de Perico"));
    }

    [Fact]
    public void FormatUpcomingOgDescription_OmitsMidnightAsKickoffTime()
    {
        var match = UpcomingMatch();
        match.Kickoff = new DateTime(2026, 8, 29);

        Assert.Equal("29 AGO · Cancha F", TeamDisplayHelper.FormatUpcomingOgDescription(match, null));
    }

    private static MatchViewModel UpcomingMatch() => new()
    {
        Kickoff = new DateTime(2026, 8, 29, 13, 10, 0),
        FieldName = "F",
        HomeTeam = new TeamViewModel { Name = "DEP ELUNEY POPULAR" },
        AwayTeam = new TeamViewModel { Name = "JUEVES DE SOMETIDOS" }
    };
}
