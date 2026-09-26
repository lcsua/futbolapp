using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Tests;

public class MatchSlugHelperTests
{
    [Fact]
    public void FromNames_UsesHyphensAndVs()
    {
        var slug = MatchSlugHelper.FromNames("JUEVES DE SOMETIDOS", "DEP CTJ");
        Assert.Equal("jueves-de-sometidos-vs-dep-ctj", slug);
    }

    [Fact]
    public void FromMatch_AppendsSeasonWhenPresent()
    {
        var match = new MatchViewModel
        {
            Id = Guid.Parse("7d857564-0eb7-493e-9976-ce17e0d84e5b"),
            SeasonSlug = "clausura-2026",
            HomeTeam = new TeamViewModel { Name = "JUEVES DE SOMETIDOS", Slug = "jueves-de-sometidos" },
            AwayTeam = new TeamViewModel { Name = "DEP CTJ", Slug = "dep-ctj" }
        };

        Assert.Equal("jueves-de-sometidos-vs-dep-ctj-clausura-2026", MatchSlugHelper.FromMatch(match));
        Assert.Equal("~/partido/jueves-de-sometidos-vs-dep-ctj-clausura-2026", MatchSlugHelper.AppRelative(match));
    }

    [Fact]
    public void FromMatch_FallsBackToTeamNameWhenSlugIsGuid()
    {
        var match = new MatchViewModel
        {
            SeasonSlug = "Clausura 2026",
            HomeTeam = new TeamViewModel
            {
                Name = "ATL FOR EVER",
                Slug = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
            },
            AwayTeam = new TeamViewModel { Name = "AC. LA UNION", Slug = "ac-la-union" }
        };

        Assert.Equal("atl-for-ever-vs-ac-la-union-clausura-2026", MatchSlugHelper.FromMatch(match));
    }

    [Fact]
    public void FromMatch_OmitsSeasonWhenMissing()
    {
        var match = new MatchViewModel
        {
            HomeTeam = new TeamViewModel { Name = "Local", Slug = "local" },
            AwayTeam = new TeamViewModel { Name = "Visitante", Slug = "visitante" }
        };

        Assert.Equal("local-vs-visitante", MatchSlugHelper.FromMatch(match));
    }
}
