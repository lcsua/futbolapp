namespace PublicWeb.Seo;

public static class PublicShareLinks
{
    public static string SectionPath(string leagueSlug, string section, string? seasonSlug, string divisionSlug)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(seasonSlug))
            qs.Add($"season={Uri.EscapeDataString(seasonSlug)}");
        qs.Add($"division={Uri.EscapeDataString(divisionSlug)}");
        return $"/ligas/{leagueSlug}/{section}?{string.Join("&", qs)}";
    }

    public static string ShareCardPath(string kind, string divisionName, string leagueName, string? seasonName)
    {
        var q = QueryString.Empty
            .Add("kind", kind)
            .Add("division", divisionName)
            .Add("league", leagueName);
        if (!string.IsNullOrWhiteSpace(seasonName))
            q = q.Add("season", seasonName);
        q = q.Add("v", "3");
        return "/og/share.jpg" + q;
    }

    public static string Headline(string kind) => kind switch
    {
        "fixture" => "Fixture",
        "posiciones" => "Posiciones",
        _ => "Resultados"
    };

    /// <summary>
    /// Custom OG card when sharing a specific division (fixture, results, standings).
    /// </summary>
    public static bool UseCustomOgCard(string kind, string? divisionName)
    {
        return !string.IsNullOrWhiteSpace(divisionName);
    }
}
