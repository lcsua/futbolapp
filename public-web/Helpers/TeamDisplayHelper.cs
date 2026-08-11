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

    /// <summary>
    /// Whether <paramref name="kickoff"/> includes a defined clock time.
    /// Domain stores optional <c>Fixture.StartTime</c> (<c>TimeOnly?</c>);
    /// public DTO maps null StartTime by parsing the match date alone,
    /// which yields 00:00:00. At the public layer we treat midnight as "no time set".
    /// </summary>
    public static bool HasDefinedTime(DateTime kickoff) =>
        kickoff != default && kickoff.TimeOfDay != TimeSpan.Zero;

    /// <summary>HH:mm when defined; empty string when not (never "00:00" for unset).</summary>
    public static string FormatTime(DateTime kickoff) =>
        HasDefinedTime(kickoff) ? kickoff.ToString("HH:mm") : string.Empty;

    /// <summary>HH:mm or null when the kickoff has no defined clock time.</summary>
    public static string? FormatTimeOrNull(DateTime kickoff) =>
        HasDefinedTime(kickoff) ? kickoff.ToString("HH:mm") : null;

    /// <summary>08 AGO 2026</summary>
    public static string FormatDayMonthYear(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        return kickoff.ToString("dd MMM yyyy", EsAr).ToUpper(EsAr);
    }

    /// <summary>08 AGO</summary>
    public static string FormatDayMonth(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        return kickoff.ToString("dd MMM", EsAr).ToUpper(EsAr);
    }

    /// <summary>Sábado 15 Ago · for compact headers</summary>
    public static string FormatWeekdayDayMonth(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        var raw = kickoff.ToString("dddd d MMM", EsAr);
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(raw);
    }

    /// <summary>
    /// Normalizes field labels so codes like "A" read as "Cancha A",
    /// without duplicating the prefix when already present.
    /// </summary>
    public static string? FormatFieldLabel(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return null;
        var raw = fieldName.Trim();

        if (raw.StartsWith("Cancha ", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("Campo ", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("Estadio ", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("Complejo ", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        // Single letter/digit codes (A, B, C, 1…) → "Cancha A"
        if (raw.Length == 1 && char.IsLetterOrDigit(raw[0]))
            return $"Cancha {char.ToUpperInvariant(raw[0])}";

        // Short bare codes like "A1", "B2"
        if (raw.Length <= 3 && raw.All(c => char.IsLetterOrDigit(c)))
            return $"Cancha {raw.ToUpperInvariant()}";

        return raw;
    }
}
