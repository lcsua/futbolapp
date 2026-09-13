using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Tests;

public class LeagueThemeTests
{
    [Fact]
    public void Empty_league_emits_no_overrides()
    {
        var theme = LeagueTheme.From(new LeagueViewModel { Name = "Veteranos", Slug = "veteranos" });
        Assert.False(theme.HasCustomizations);
        Assert.Equal("", theme.InlineStyle);
        Assert.Null(theme.GoogleFontsHref);
    }

    [Fact]
    public void Default_green_and_inter_do_not_override_css()
    {
        var theme = LeagueTheme.From(new LeagueViewModel
        {
            PrimaryColor = "#16A34A",
            FontKey = "inter",
        });
        Assert.False(theme.HasCustomizations);
    }

    [Fact]
    public void Custom_color_font_and_heroes_emit_css_variables()
    {
        var theme = LeagueTheme.From(new LeagueViewModel
        {
            PrimaryColor = "#2563EB",
            FontKey = "nunito",
            HeroImageUrl = "/uploads/leagues/a/hero.jpg",
            TeamHeroImageUrl = "https://cdn.example/team.png",
        });

        Assert.True(theme.HasCustomizations);
        Assert.Contains("--v2-primary:#2563EB", theme.InlineStyle);
        Assert.Contains("--font-family:", theme.InlineStyle);
        Assert.Contains("Nunito", theme.InlineStyle);
        Assert.Contains("--league-hero-image:url(\"/uploads/leagues/a/hero.jpg\")", theme.InlineStyle);
        Assert.Contains("--team-hero-image:url(\"https://cdn.example/team.png\")", theme.InlineStyle);
        Assert.Contains("Nunito:wght@", theme.GoogleFontsHref);
    }

    [Fact]
    public void Rejects_unsafe_hero_urls()
    {
        var theme = LeagueTheme.From(new LeagueViewModel
        {
            HeroImageUrl = "javascript:alert(1)",
        });
        Assert.False(theme.HasCustomizations);
    }
}
