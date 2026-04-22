using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace FootballManager.Application.Services;

/// <summary>Parses JSON stored on <see cref="FootballManager.Domain.Entities.DivisionMatchRules"/>.</summary>
public static class SchedulingRulesJsonParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Array of GUID strings, e.g. <c>["guid1","guid2"]</c>.</summary>
    public static IReadOnlyList<Guid>? TryParseFieldIdList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var list = JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions);
            return list is { Count: > 0 } ? list : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Array of <c>{"start":"09:00","end":"13:00"}</c> (times as HH:mm or HH:mm:ss).</summary>
    public static IReadOnlyList<(TimeOnly Start, TimeOnly End)>? TryParseKickoffRanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;
            var ranges = new List<(TimeOnly, TimeOnly)>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                if (!el.TryGetProperty("start", out var sEl) || !el.TryGetProperty("end", out var eEl)) continue;
                var s = ParseTime(sEl.GetString());
                var e = ParseTime(eEl.GetString());
                if (s == null || e == null || s >= e) continue;
                ranges.Add((s.Value, e.Value));
            }

            return ranges.Count > 0 ? ranges : null;
        }
        catch
        {
            return null;
        }
    }

    private static TimeOnly? ParseTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)) return t;
        if (TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts))
            return TimeOnly.FromTimeSpan(ts);
        return null;
    }
}
