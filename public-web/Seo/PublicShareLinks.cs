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

    /// <summary>
    /// Custom OG card for fixture/results of any division, and standings only for División A.
    /// </summary>
    public static bool UseCustomOgCard(string kind, string? divisionName)
    {
        if (string.IsNullOrWhiteSpace(divisionName)) return false;
        if (!string.Equals(kind, "posiciones", StringComparison.OrdinalIgnoreCase))
            return true;
        return IsDivisionA(divisionName);
    }

    public static bool IsDivisionA(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.Trim();
        if (n.Equals("A", StringComparison.OrdinalIgnoreCase)) return true;
        if (n.Equals("a", StringComparison.OrdinalIgnoreCase)) return true;
        return n.Equals("División A", StringComparison.OrdinalIgnoreCase)
            || n.Equals("Division A", StringComparison.OrdinalIgnoreCase);
    }
}
