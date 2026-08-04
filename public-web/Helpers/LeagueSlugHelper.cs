using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PublicWeb.Helpers;

/// <summary>
/// Minimal slug helpers for legacy URL redirects (keeps public-web independent from backend).
/// </summary>
public static class LeagueSlugHelper
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphens = new(@"-+", RegexOptions.Compiled);
    private static readonly HashSet<string> NoiseTokens = new(StringComparer.OrdinalIgnoreCase) { "liga", "ligas" };

    public static string CleanLeagueSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        var normalized = Normalize(slug);
        if (string.IsNullOrEmpty(normalized))
            return string.Empty;

        var tokens = normalized
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => !NoiseTokens.Contains(t))
            .ToArray();

        return tokens.Length == 0 ? normalized : string.Join('-', tokens);
    }

    private static string Normalize(string input)
    {
        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-')
                sb.Append('-');
        }

        var result = InvalidChars.Replace(sb.ToString(), string.Empty);
        result = MultipleHyphens.Replace(result, "-");
        return result.Trim('-');
    }
}
