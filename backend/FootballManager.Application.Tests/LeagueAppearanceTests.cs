using FootballManager.Domain.Entities;

namespace FootballManager.Application.Tests;

public class LeagueAppearanceTests
{
    [Fact]
    public void Empty_values_stay_null_so_product_defaults_apply()
    {
        Assert.Null(LeagueAppearance.NormalizeColor(null));
        Assert.Null(LeagueAppearance.NormalizeColor(""));
        Assert.Null(LeagueAppearance.NormalizeColor("  "));
        Assert.Null(LeagueAppearance.NormalizeFontKey(null));
        Assert.Null(LeagueAppearance.NormalizeFontKey("inter"));
        Assert.Null(LeagueAppearance.NormalizeFontKey("Inter"));
        Assert.Null(LeagueAppearance.NormalizeImageUrl(""));
    }

    [Fact]
    public void Default_green_is_not_stored()
    {
        Assert.Null(LeagueAppearance.NormalizeColor("#16A34A"));
        Assert.Null(LeagueAppearance.NormalizeColor("16a34a"));
        Assert.Null(LeagueAppearance.NormalizeColor("#16a34a"));
    }

    [Fact]
    public void Custom_hex_is_normalized()
    {
        Assert.Equal("#1D4ED8", LeagueAppearance.NormalizeColor("#1d4ed8"));
        Assert.Equal("#FF8800", LeagueAppearance.NormalizeColor("f80"));
    }

    [Fact]
    public void Rejects_invalid_color_and_font()
    {
        Assert.Throws<ArgumentException>(() => LeagueAppearance.NormalizeColor("green"));
        Assert.Throws<ArgumentException>(() => LeagueAppearance.NormalizeFontKey("comic-sans"));
    }

    [Fact]
    public void UpdateAppearance_on_a_league_leaves_defaults_empty()
    {
        var league = new League("Liga Test", "AR", "liga-test");
        league.UpdateAppearance("#16A34A", "inter", "  ", null);
        Assert.Null(league.PrimaryColor);
        Assert.Null(league.FontKey);
        Assert.Null(league.HeroImageUrl);
        Assert.Null(league.TeamHeroImageUrl);
    }

    [Fact]
    public void UpdateAppearance_stores_custom_skin()
    {
        var league = new League("Liga Test", "AR", "liga-test");
        league.UpdateAppearance("#2563EB", "nunito", "/uploads/leagues/x/hero.jpg", "https://cdn.example/team.webp");
        Assert.Equal("#2563EB", league.PrimaryColor);
        Assert.Equal("nunito", league.FontKey);
        Assert.Equal("/uploads/leagues/x/hero.jpg", league.HeroImageUrl);
        Assert.Equal("https://cdn.example/team.webp", league.TeamHeroImageUrl);
    }
}
