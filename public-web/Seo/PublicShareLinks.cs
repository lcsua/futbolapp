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
        return "/og/share.png" + q;
    }

    public static string Headline(string kind) => kind switch
    {
        "fixture" => "Fixture",
        "posiciones" => "Posiciones",
        _ => "Resultados"
    };
}
