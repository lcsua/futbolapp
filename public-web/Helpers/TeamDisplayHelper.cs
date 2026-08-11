using System.Globalization;
using System.Text.RegularExpressions;
using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

public static class TeamDisplayHelper
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    private static readonly HashSet<string> InitialStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DE", "DEL", "Y", "E", "DA", "DO", "DI", "DAS", "DOS",
        "F.C", "F.C.", "FC", "CLUB"
    };

    public static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";

        var cleaned = name.Trim()
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal);

        var parts = cleaned
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !InitialStopWords.Contains(p))
            .Where(p => !p.All(char.IsDigit))
            .ToList();

        if (parts.Count == 0)
        {
            var fallback = name.Trim();
            return fallback.Substring(0, Math.Min(2, fallback.Length)).ToUpperInvariant();
        }

        if (parts.Count == 1)
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpperInvariant();

        return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
    }

    /// <summary>
    /// Short label for a division badge (e.g. "45", "+35", "U17", "PR").
    /// Returns null when no compact abbreviation is sensible (caller shows icon).
    /// </summary>
    public static string? GetDivisionAbbrev(string? divisionName)
    {
        if (string.IsNullOrWhiteSpace(divisionName)) return null;
        var raw = divisionName.Trim();

        // "45", "+35", "45 - ZONA A"
        var age = Regex.Match(raw, @"^(\+?\d+)\b");
        if (age.Success) return age.Groups[1].Value;

        // "Sub 17", "Sub-17", "U17", "U-17"
        var sub = Regex.Match(raw, @"^(?:Sub|U)[\s\-–—]?(\d+)\b", RegexOptions.IgnoreCase);
        if (sub.Success) return "U" + sub.Groups[1].Value;

        var firstSegment = Regex.Split(raw, @"\s*[-–—]\s*")[0].Trim();
        var key = firstSegment.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? firstSegment;
        var keyLower = key.ToLowerInvariant();

        if (keyLower.StartsWith("primera", StringComparison.Ordinal)) return "PR";
        if (keyLower.StartsWith("femenin", StringComparison.Ordinal)) return "FE";
        if (keyLower.StartsWith("masculin", StringComparison.Ordinal)) return "MA";
        if (keyLower.StartsWith("reserva", StringComparison.Ordinal)) return "RE";
        if (keyLower.StartsWith("juvenil", StringComparison.Ordinal)) return "JU";
        if (keyLower.StartsWith("senior", StringComparison.Ordinal) || keyLower.StartsWith("sénior", StringComparison.Ordinal)) return "SR";
        if (keyLower.StartsWith("veteran", StringComparison.Ordinal)) return "VT";
        if (keyLower.StartsWith("libre", StringComparison.Ordinal)) return "LI";

        // Compact first token (≤4 alphanumeric / +)
        if (key.Length is > 0 and <= 4 && key.All(c => char.IsLetterOrDigit(c) || c == '+'))
            return key.ToUpperInvariant();

        // Two letters from a longer first word
        var chars = key.Where(char.IsLetterOrDigit).Take(2).ToArray();
        if (chars.Length >= 2)
            return $"{char.ToUpperInvariant(chars[0])}{char.ToUpperInvariant(chars[1])}";

        return null;
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
