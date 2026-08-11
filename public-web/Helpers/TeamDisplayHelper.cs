using System.Globalization;
using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

public static class TeamDisplayHelper
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();
        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    public static bool IsFinished(string? status)
    {
        var s = (status ?? string.Empty).Trim().ToUpperInvariant();
        return s is "COMPLETED" or "PLAYED" or "FINISHED" or "SUSPENDED";
    }

    public static bool IsSuspended(string? status) =>
        string.Equals(status, "SUSPENDED", StringComparison.OrdinalIgnoreCase);

    /// <summary>G / E / P relative to focus team; null if not a completed result.</summary>
    public static char? FormLetter(MatchViewModel match, Guid focusTeamId)
    {
        if (match == null || IsSuspended(match.Status)) return null;
        var s = (match.Status ?? string.Empty).Trim().ToUpperInvariant();
        if (s is not ("COMPLETED" or "PLAYED" or "FINISHED")) return null;
        if (!match.HomeScore.HasValue || !match.AwayScore.HasValue) return null;

        var isHome = match.HomeTeam.Id == focusTeamId;
        var isAway = match.AwayTeam.Id == focusTeamId;
        if (!isHome && !isAway) return null;

        var forUs = isHome ? match.HomeScore.Value : match.AwayScore.Value;
        var against = isHome ? match.AwayScore.Value : match.HomeScore.Value;
        if (forUs > against) return 'G';
        if (forUs < against) return 'P';
        return 'E';
    }

    public static string FormatLongDate(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        var raw = kickoff.ToString("dddd d MMM yyyy", EsAr);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw);
    }

    public static string FormatShortDate(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        var raw = kickoff.ToString("dddd d MMM", EsAr);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw);
    }

    public static string FormatTime(DateTime kickoff) =>
        kickoff == default ? "—" : kickoff.ToString("HH:mm");
}
