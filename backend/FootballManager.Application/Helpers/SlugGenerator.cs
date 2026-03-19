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
