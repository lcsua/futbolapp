using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FootballManager.Application.Helpers;

/// <summary>
/// Generates URL-friendly slugs from text.
/// Rules: lowercase, replace spaces with -, remove special chars (only a-z, 0-9, -).
/// Normalizes accents (á→a, ñ→n, etc.).
/// </summary>
public static class SlugGenerator
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphens = new(@"-+", RegexOptions.Compiled);
    private static readonly HashSet<string> LeagueNoiseTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "liga",
        "ligas"
    };

    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant();
        var decomposed = normalized.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            var normalizedChar = c.ToString().Normalize(NormalizationForm.FormC);
            if (char.IsLetterOrDigit(normalizedChar[0]))
            {
                sb.Append(RemoveAccent(normalizedChar[0]));
            }
            else if (char.IsWhiteSpace(c) || c == '-')
            {
                sb.Append('-');
            }
        }

        var result = sb.ToString();
        result = InvalidChars.Replace(result, string.Empty);
        result = MultipleHyphens.Replace(result, "-");
        result = result.Trim('-');

        return result;
    }

    /// <summary>
    /// League public URLs already live under /ligas, so strip redundant "liga/ligas" tokens from the slug.
    /// Example: "Liga Infantil de Perico" → "infantil-de-perico".
    /// </summary>
    public static string GenerateLeagueSlug(string input)
    {
        return CleanLeagueSlug(Generate(input));
    }

    /// <summary>
    /// Removes hyphen-separated "liga" / "ligas" tokens from an existing slug.
    /// Falls back to the original slug if cleaning would leave it empty.
    /// </summary>
    public static string CleanLeagueSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        var normalized = Generate(slug);
        if (string.IsNullOrEmpty(normalized))
            return string.Empty;

        var tokens = normalized
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !LeagueNoiseTokens.Contains(t))
            .ToArray();

        if (tokens.Length == 0)
            return normalized;

        return string.Join('-', tokens);
    }

    /// <summary>
    /// Public match URLs: {home-slug}-vs-{away-slug} plus optional -{season-slug}
    /// so the same pairing in another tournament stays unique.
    /// </summary>
    public static string GenerateMatchSlug(string home, string away, string? season = null)
    {
        var homeSlug = Generate(home);
        var awaySlug = Generate(away);
        if (string.IsNullOrEmpty(homeSlug) || string.IsNullOrEmpty(awaySlug))
            return string.Empty;

        var slug = $"{homeSlug}-vs-{awaySlug}";
        var seasonSlug = Generate(season ?? string.Empty);
        if (!string.IsNullOrEmpty(seasonSlug))
            slug += $"-{seasonSlug}";
        return slug;
    }

    /// <summary>
    /// Splits {home}-vs-{away} or {home}-vs-{away}-{season}.
    /// <paramref name="awayAndSeason"/> is everything after the first -vs-;
    /// the repository matches the longest away slug plus optional season suffix.
    /// </summary>
    public static bool TryParseMatchSlug(string? slug, out string homeSlug, out string awayAndSeason)
    {
        homeSlug = string.Empty;
        awayAndSeason = string.Empty;

        var normalized = Generate(slug ?? string.Empty);
        if (string.IsNullOrEmpty(normalized))
            return false;

        const string separator = "-vs-";
        var idx = normalized.IndexOf(separator, StringComparison.Ordinal);
        if (idx <= 0)
            return false;

        homeSlug = normalized[..idx];
        awayAndSeason = normalized[(idx + separator.Length)..];
        return homeSlug.Length > 0 && awayAndSeason.Length > 0;
    }

    private static char RemoveAccent(char c)
    {
        var s = c.ToString().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in s)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.Length > 0 ? sb[0] : c;
    }
}
