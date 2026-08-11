using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FootballManager.Application.Helpers;

/// <summary>
/// Shared team-name normalization for CSV imports (mirrors frontend-admin teamNameMatch).
/// </summary>
public static class TeamNameNormalizer
{
    private static readonly (Regex Pattern, string Replacement)[] Abbreviations =
    {
        (new Regex(@"(^|[\s/])B[º°O]\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1BARRIO"),
        (new Regex(@"(^|[\s/])ATL\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1ATLETICO"),
        (new Regex(@"(^|[\s/])DEF\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1DEFENSORES"),
        (new Regex(@"(^|[\s/])DEP\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1DEPORTIVO"),
        (new Regex(@"(^|[\s/])STO\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1SANTO"),
        (new Regex(@"(^|[\s/])F\.?\s*C\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1FC"),
        (new Regex(@"(^|[\s/])C\.?\s*A\.?(?=[\s/]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant), "$1CA"),
    };

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var s = input.Trim().ToUpperInvariant();
        s = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(c);
        }
        s = sb.ToString().Normalize(NormalizationForm.FormC);

        foreach (var (pattern, replacement) in Abbreviations)
            s = pattern.Replace(s, replacement);

        // Drop quotes/punctuation so CENTRAL NORTE "A" SENIOR == CENTRAL NORTE A SENIOR
        s = Regex.Replace(s, "['\"\u201C\u201D\u00B4`]", "");
        s = Regex.Replace(s, @"[^A-Z0-9\s]", " ");
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    public static bool EqualsNormalized(string? a, string? b)
        => Normalize(a) == Normalize(b) && !string.IsNullOrEmpty(Normalize(a));
}
