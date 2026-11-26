namespace PublicWeb.Helpers;

public static class LeagueLinks
{
    public static string Section(
        string leagueSlug,
        string section,
        string? season = null,
        string? division = null,
        string? teamSlug = null)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(season))
            parts.Add("season=" + Uri.EscapeDataString(season.Trim()));
        if (!string.IsNullOrWhiteSpace(division)
            && !division.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            parts.Add("division=" + Uri.EscapeDataString(division.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(teamSlug))
            parts.Add("equipo=" + Uri.EscapeDataString(teamSlug.Trim()));

        var path = string.IsNullOrWhiteSpace(section)
            ? $"~/ligas/{leagueSlug}"
            : $"~/ligas/{leagueSlug}/{section}";
        return parts.Count == 0 ? path : path + "?" + string.Join("&", parts);
    }
}
