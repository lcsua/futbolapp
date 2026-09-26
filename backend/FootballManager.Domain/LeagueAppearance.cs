using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Normalizes optional public-site theming. Empty / default green / Inter stay null
/// so existing leagues keep the product look without storing a custom skin.
/// </summary>
public static class LeagueAppearance
{
    public const string DefaultPrimaryColor = "#16A34A";
    public const string DefaultFontKey = "inter";
    public const int MaxImageUrlLength = 1000;

    public static readonly IReadOnlyList<string> FontKeys = new[]
    {
        DefaultFontKey,
        "barlow",
        "nunito",
        "rubik",
    };

    private static readonly HashSet<string> FontKeySet = new(FontKeys, StringComparer.OrdinalIgnoreCase);
    private static readonly Regex HexColor = new(@"^#?([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6})$", RegexOptions.Compiled);

    public static string? NormalizeColor(string? value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (trimmed == null)
            return null;

        var match = HexColor.Match(trimmed);
        if (!match.Success)
            throw new ArgumentException("Primary color must be a hex value like #16A34A.");

        var hex = match.Groups[1].Value;
        if (hex.Length == 3)
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);

        var normalized = "#" + hex.ToUpperInvariant();
        if (string.Equals(normalized, DefaultPrimaryColor, StringComparison.OrdinalIgnoreCase))
            return null;

        return normalized;
    }

    public static string? NormalizeFontKey(string? value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        if (trimmed == null || trimmed == DefaultFontKey)
            return null;

        if (!FontKeySet.Contains(trimmed))
            throw new ArgumentException("Font is not one of the allowed presets.");

        return trimmed;
    }

    public static string? NormalizeImageUrl(string? value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (trimmed == null)
            return null;

        if (trimmed.Length > MaxImageUrlLength)
            throw new ArgumentException("Image URL is too long.");

        return trimmed;
    }
}
