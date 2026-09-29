using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Helpers;

/// <summary>
/// Zones of the same category are separate divisions whose names only differ by the trailing
/// "Zona X" ("Sub 13 Zona A", "Sub 13 - Zona B"). Keep in sync with frontend-admin utils/zones.ts.
/// </summary>
public static class DivisionCategory
{
    private static readonly Regex ZoneSuffix = new(@"\s*[-—–]?\s*zona\s+\S+\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Key(string divisionName) =>
        ZoneSuffix.Replace(divisionName ?? string.Empty, string.Empty).Trim().ToLowerInvariant();

    public static bool IsZone(string divisionName) =>
        ZoneSuffix.IsMatch(divisionName ?? string.Empty);

    /// <summary>Other zones of the same category in the season (empty when the division is not a zone).</summary>
    public static List<DivisionSeason> SiblingZones(DivisionSeason divisionSeason, IEnumerable<DivisionSeason> seasonDivisions)
    {
        var name = divisionSeason.Division?.Name ?? string.Empty;
        if (!IsZone(name))
            return new List<DivisionSeason>();

        var key = Key(name);
        return seasonDivisions
            .Where(ds => ds.Id != divisionSeason.Id
                         && ds.Division != null
                         && IsZone(ds.Division.Name)
                         && Key(ds.Division.Name) == key)
            .ToList();
    }
}
