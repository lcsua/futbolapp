using System.Globalization;
using System.Text.Json;
using FootballManager.Application.ProfessionalFootball;

namespace FootballManager.Infrastructure.ProfessionalFootball;

public static class EspnSeasonsParser
{
    public static IReadOnlyList<SeasonInfo> Parse(JsonElement root)
    {
        if (!root.TryGetProperty("seasons", out var seasonsEl) || seasonsEl.ValueKind != JsonValueKind.Array)
            return Array.Empty<SeasonInfo>();

        var list = new List<SeasonInfo>();
        foreach (var season in seasonsEl.EnumerateArray())
        {
            var year = season.TryGetProperty("year", out var y) ? y.GetInt32() : 0;
            var types = new List<SeasonTypeInfo>();
            if (season.TryGetProperty("types", out var typesEl) && typesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var t in typesEl.EnumerateArray())
                {
                    var id = t.TryGetProperty("id", out var idEl) ? idEl.ToString() : "";
                    var name = t.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var start = ParseDate(t, "startDate");
                    var end = ParseDate(t, "endDate");
                    bool? hasStandings = null;
                    if (t.TryGetProperty("hasStandings", out var hs))
                    {
                        if (hs.ValueKind is JsonValueKind.True or JsonValueKind.False)
                            hasStandings = hs.GetBoolean();
                    }

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                        continue;

                    types.Add(new SeasonTypeInfo(id, name, start, end, hasStandings));
                }
            }

            list.Add(new SeasonInfo(year, types));
        }

        return list;
    }

    private static DateTimeOffset ParseDate(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return DateTimeOffset.MinValue;
        var s = p.GetString();
        if (string.IsNullOrWhiteSpace(s))
            return DateTimeOffset.MinValue;
        return DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)
            ? dto
            : DateTimeOffset.MinValue;
    }
}
