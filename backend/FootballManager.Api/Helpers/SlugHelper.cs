using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FootballManager.Api.Helpers;

public static class SlugHelper
{
    public static string NormalizeSlug(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Convert to lowercase
        var normalized = input.ToLowerInvariant();

        // Remove accents (diacritics)
        normalized = RemoveDiacritics(normalized);

        // Replace spaces with hyphens
        normalized = normalized.Replace(" ", "-");

        // Remove invalid characters
        normalized = Regex.Replace(normalized, @"[^a-z0-9\-_]", "");

        // Collapse multiple hyphens
        normalized = Regex.Replace(normalized, @"-+", "-");

        // Trim hyphens from start and end
        return normalized.Trim('-');
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
