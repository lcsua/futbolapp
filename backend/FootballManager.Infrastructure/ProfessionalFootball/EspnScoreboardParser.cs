using System.Globalization;
using System.Text.Json;
using FootballManager.Application.ProfessionalFootball;

namespace FootballManager.Infrastructure.ProfessionalFootball;

public static class EspnScoreboardParser
{
    public static IReadOnlyList<ProfessionalMatchDto> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return Array.Empty<ProfessionalMatchDto>();

        var list = new List<ProfessionalMatchDto>();
        foreach (var ev in events.EnumerateArray())
        {
            var id = ev.TryGetProperty("id", out var idEl) ? idEl.ToString() : "";
            var dateStr = ev.TryGetProperty("date", out var d) ? d.GetString() : null;
            if (!DateTimeOffset.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
                continue;

            string status = "pre";
            string statusDetail = "";
            if (ev.TryGetProperty("status", out var st) && st.TryGetProperty("type", out var typ))
            {
                status = typ.TryGetProperty("state", out var state) ? state.GetString() ?? "pre" : "pre";
                if (typ.TryGetProperty("description", out var desc) && !string.IsNullOrWhiteSpace(desc.GetString()))
                    statusDetail = desc.GetString()!;
                else if (typ.TryGetProperty("name", out var nm))
                    statusDetail = nm.GetString() ?? "";
            }

            string? venue = null;
            TeamSummaryDto? home = null;
            TeamSummaryDto? away = null;
            int? homeScore = null;
            int? awayScore = null;

            if (ev.TryGetProperty("competitions", out var comps)
                && comps.ValueKind == JsonValueKind.Array
                && comps.GetArrayLength() > 0)
            {
                var comp = comps[0];
                if (comp.TryGetProperty("venue", out var v) && v.TryGetProperty("fullName", out var vn))
                    venue = vn.GetString();

                if (comp.TryGetProperty("competitors", out var competitors) && competitors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in competitors.EnumerateArray())
                    {
                        var team = ParseTeam(c);
                        int? score = null;
                        if (c.TryGetProperty("score", out var sc))
                        {
                            if (sc.ValueKind == JsonValueKind.Number)
                                score = sc.GetInt32();
                            else if (sc.ValueKind == JsonValueKind.String && int.TryParse(sc.GetString(), out var si))
                                score = si;
                        }

                        var homeAway = c.TryGetProperty("homeAway", out var ha) ? ha.GetString() : null;
                        if (string.Equals(homeAway, "home", StringComparison.OrdinalIgnoreCase))
                        {
                            home = team;
                            homeScore = score;
                        }
                        else
                        {
                            away = team;
                            awayScore = score;
                        }
                    }
                }
            }

            if (home == null || away == null)
                continue;

            list.Add(new ProfessionalMatchDto(
                id, date, status, statusDetail, home, away, venue, homeScore, awayScore));
        }

        return list.OrderBy(m => m.Date).ToList();
    }

    private static TeamSummaryDto ParseTeam(JsonElement competitor)
    {
        string id = "";
        string name = "";
        string? logo = null;
        if (competitor.TryGetProperty("team", out var team))
        {
            id = team.TryGetProperty("id", out var tid) ? tid.ToString() : "";
            if (team.TryGetProperty("displayName", out var dn) && !string.IsNullOrWhiteSpace(dn.GetString()))
                name = dn.GetString()!;
            else if (team.TryGetProperty("shortDisplayName", out var sd))
                name = sd.GetString() ?? "";

            if (team.TryGetProperty("logo", out var logoEl))
                logo = logoEl.GetString();
            else if (team.TryGetProperty("logos", out var logos)
                && logos.ValueKind == JsonValueKind.Array
                && logos.GetArrayLength() > 0
                && logos[0].TryGetProperty("href", out var href))
            {
                logo = href.GetString();
            }
        }

        return new TeamSummaryDto(id, name, logo);
    }
}
