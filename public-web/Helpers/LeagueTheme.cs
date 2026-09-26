using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

/// <summary>
/// Builds optional CSS overrides for a league. Empty appearance leaves the V2 defaults
/// (green, Inter, product hero photos) untouched.
/// </summary>
public static class LeagueTheme
{
    public const string DefaultFontKey = "inter";

    private static readonly Dictionary<string, FontPreset> Fonts = new(StringComparer.OrdinalIgnoreCase)
    {
        [DefaultFontKey] = new("\"Inter\", system-ui, -apple-system, BlinkMacSystemFont, \"Segoe UI\", sans-serif", null),
        ["barlow"] = new("\"Barlow\", \"Inter\", system-ui, sans-serif", "Barlow:wght@400;500;600;700;800"),
        ["nunito"] = new("\"Nunito\", \"Inter\", system-ui, sans-serif", "Nunito:wght@400;500;600;700;800"),
        ["rubik"] = new("\"Rubik\", \"Inter\", system-ui, sans-serif", "Rubik:wght@400;500;600;700;800"),
    };

    public static LeagueThemeModel From(LeagueViewModel? league)
    {
        if (league == null)
            return LeagueThemeModel.Empty;

        var color = NormalizeColor(league.PrimaryColor);
        if (string.Equals(color, "#16A34A", StringComparison.OrdinalIgnoreCase))
            color = null;
        var fontKey = string.IsNullOrWhiteSpace(league.FontKey)
            ? null
            : league.FontKey.Trim().ToLowerInvariant();
        if (fontKey == DefaultFontKey || fontKey != null && !Fonts.ContainsKey(fontKey))
            fontKey = null;

        Fonts.TryGetValue(fontKey ?? DefaultFontKey, out var font);
        var hero = CssImageUrl(league.HeroImageUrl);
        var teamHero = CssImageUrl(league.TeamHeroImageUrl);

        if (color == null && fontKey == null && hero == null && teamHero == null)
            return LeagueThemeModel.Empty;

        var vars = new List<string>();
        if (color != null)
        {
            vars.Add($"--v2-primary:{color}");
            vars.Add($"--v2-primary-dark:color-mix(in srgb, {color} 78%, #000)");
            vars.Add($"--v2-accent:color-mix(in srgb, {color} 42%, #fff)");
        }

        if (fontKey != null && font != null)
            vars.Add($"--font-family:{font.FamilyStack}");

        if (hero != null)
            vars.Add($"--league-hero-image:{hero}");

        if (teamHero != null)
            vars.Add($"--team-hero-image:{teamHero}");

        return new LeagueThemeModel
        {
            GoogleFontsHref = font?.GoogleQuery == null
                ? null
                : "https://fonts.googleapis.com/css2?family=" + font.GoogleQuery + "&display=swap",
            InlineStyle = string.Join(";", vars) + ";"
        };
    }

    private static string? NormalizeColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var t = value.Trim();
        if (t[0] != '#')
            t = "#" + t;

        if (t.Length == 4)
            t = "#" + t[1] + t[1] + t[2] + t[2] + t[3] + t[3];

        if (t.Length != 7)
            return null;

        for (var i = 1; i < t.Length; i++)
        {
            var c = t[i];
            var hex = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
            if (!hex)
                return null;
        }

        return t.ToUpperInvariant();
    }

    private static string? CssImageUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var url = raw.Trim();
        var isHttp = url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
        var isPath = url.StartsWith('/');
        if (!isHttp && !isPath)
            return null;

        var escaped = url.Replace("\\", "/", StringComparison.Ordinal).Replace("\"", "%22", StringComparison.Ordinal);
        return $"url(\"{escaped}\")";
    }

    private sealed record FontPreset(string FamilyStack, string? GoogleQuery);
}

public sealed class LeagueThemeModel
{
    public static readonly LeagueThemeModel Empty = new();

    public string? GoogleFontsHref { get; init; }
    public string InlineStyle { get; init; } = "";

    public bool HasCustomizations => !string.IsNullOrWhiteSpace(InlineStyle);
}
