using System.Globalization;
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

    /// <summary>
    /// Articles, club suffixes and generic prefixes that should not be the
    /// compact label on a narrow standings table (e.g. "LA" from "LA BARRA FC").
    /// </summary>
    private static readonly HashSet<string> CompactSkipTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "LA", "EL", "LOS", "LAS", "LO", "DE", "DEL", "Y", "E",
        "DA", "DO", "DI", "DAS", "DOS", "A",
        "FC", "F.C", "F.C.", "CF", "C.F", "C.F.", "CLUB", "AFC",
        "CA", "C.A", "C.A.",
        "ATL", "ATL.", "ATLETICO", "ATLÉTICO",
        "DEF", "DEF.", "DEFENSORES",
        "DEP", "DEPORTIVO", "SP", "SPORTIVO",
        "CS", "C.S", "C.S.", "CSD", "CSC",
        "SOCIAL", "ASOC", "ASOCIACION", "ASOCIACIÓN"
    };

    public static bool HasRealLogo(TeamViewModel? team)
    {
        var src = !string.IsNullOrWhiteSpace(team?.LogoThumbUrl)
            ? team!.LogoThumbUrl
            : team?.LogoUrl;
        return !string.IsNullOrWhiteSpace(src)
            && !src.Contains("default-team", StringComparison.OrdinalIgnoreCase);
    }

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
    /// Compact label for tight standings columns. Ignores API 3-letter
    /// ShortName fallbacks like "LA " and keeps the distinctive words.
    /// </summary>
    public static string GetCompactName(string? name, string? shortName = null)
    {
        var full = (name ?? string.Empty).Trim();
        if (full.Length == 0) return "—";

        if (IsUsefulShortName(full, shortName))
            return shortName!.Trim();

        var parts = full
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !CompactSkipTokens.Contains(p))
            .Where(p => !p.All(char.IsDigit))
            .ToList();

        if (parts.Count == 0) return full;
        return string.Join(" ", parts);
    }

    private static bool IsUsefulShortName(string fullName, string? shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName)) return false;

        var compact = shortName.Trim();
        if (compact.Length < 3) return false;
        if (CompactSkipTokens.Contains(compact)) return false;
        if (compact.Equals(fullName, StringComparison.OrdinalIgnoreCase)) return false;

        // Public API fills empty ShortName with the first 3 characters ("LA ").
        var fallback = fullName.Substring(0, Math.Min(fullName.Length, 3));
        if (compact.Equals(fallback, StringComparison.OrdinalIgnoreCase) ||
            compact.Equals(fallback.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool IsFinished(string? status)
    {
        var s = (status ?? string.Empty).Trim().ToUpperInvariant();
        return s is "COMPLETED" or "PLAYED" or "FINISHED" or "SUSPENDED";
    }

    public static bool IsSuspended(string? status) =>
        string.Equals(status, "SUSPENDED", StringComparison.OrdinalIgnoreCase);

    public static string FormatStatusLabel(string? status)
    {
        var s = (status ?? string.Empty).Trim().ToUpperInvariant();
        return s switch
        {
            "COMPLETED" or "PLAYED" or "FINISHED" => "Finalizado",
            "IN_PROGRESS" => "En juego",
            "SUSPENDED" => "Suspendido",
            "CANCELLED" => "Cancelado",
            "POSTPONED" => "Aplazado",
            _ => "Programado"
        };
    }

    public static string FormatIncidentLabel(string? type) =>
        (type ?? string.Empty).Trim() switch
        {
            "Goal" => "Gol",
            "YellowCard" => "Amarilla",
            "RedCard" => "Roja",
            "Substitution" => "Cambio",
            "Injury" => "Lesión",
            _ => "Incidencia"
        };

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

    /// <summary>Sábado 22 de agosto</summary>
    public static string FormatWeekdayDayDeMonth(DateTime kickoff)
    {
        if (kickoff == default) return "—";
        var weekday = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(kickoff.ToString("dddd", EsAr));
        var month = kickoff.ToString("MMMM", EsAr);
        return $"{weekday} {kickoff.Day} de {month}";
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
