using System.Globalization;
using System.Text.Json;
using FootballManager.Application.ProfessionalFootball;

namespace FootballManager.Infrastructure.ProfessionalFootball;

public static class EspnStandingsParser
{
    public static IReadOnlyList<StandingGroupDto> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("children", out var children) || children.ValueKind != JsonValueKind.Array)
            return Array.Empty<StandingGroupDto>();

        var groups = new List<StandingGroupDto>();
        foreach (var child in children.EnumerateArray())
        {
            var name = child.TryGetProperty("name", out var n) ? n.GetString() ?? "Grupo" : "Grupo";
            var entries = new List<StandingEntryDto>();

            if (child.TryGetProperty("standings", out var standings)
                && standings.TryGetProperty("entries", out var entriesEl)
                && entriesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in entriesEl.EnumerateArray())
                    entries.Add(ParseEntry(entry));
            }

            entries = entries.OrderBy(e => e.Position).ToList();
            groups.Add(new StandingGroupDto(name, entries));
        }

        return groups;
    }

    private static StandingEntryDto ParseEntry(JsonElement entry)
    {
        string teamId = "";
        string teamName = "";
        string? logo = null;
        if (entry.TryGetProperty("team", out var team))
        {
            teamId = team.TryGetProperty("id", out var id) ? id.ToString() : "";
            if (team.TryGetProperty("displayName", out var dn) && !string.IsNullOrWhiteSpace(dn.GetString()))
                teamName = dn.GetString()!;
            else if (team.TryGetProperty("name", out var nm))
                teamName = nm.GetString() ?? "";

            if (team.TryGetProperty("logos", out var logos) && logos.ValueKind == JsonValueKind.Array && logos.GetArrayLength() > 0
                && logos[0].TryGetProperty("href", out var href))
            {
                logo = href.GetString();
            }
        }

        var stats = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (entry.TryGetProperty("stats", out var statsEl) && statsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in statsEl.EnumerateArray())
            {
                var name = s.TryGetProperty("name", out var sn) ? sn.GetString() : null;
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                double value = 0;
                if (s.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number)
                    value = v.GetDouble();
                else if (s.TryGetProperty("displayValue", out var dv)
                    && double.TryParse(dv.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    value = parsed;
                stats[name] = value;
            }
        }

        int Get(string key) => stats.TryGetValue(key, out var x) ? (int)Math.Round(x) : 0;

        return new StandingEntryDto(
            Position: Get("rank"),
            TeamExternalId: teamId,
            TeamName: teamName,
            TeamLogo: logo,
            Played: Get("gamesPlayed"),
            Won: Get("wins"),
            Drawn: Get("ties"),
            Lost: Get("losses"),
            GoalsFor: Get("pointsFor"),
            GoalsAgainst: Get("pointsAgainst"),
            GoalDifference: Get("pointDifferential"),
            Points: Get("points"));
    }
}
