using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

/// <summary>
/// Builds a full fixture calendar by merging published results + open matches,
/// without changing V1 endpoint semantics (results vs próximos).
/// </summary>
public static class FixtureCalendarHelper
{
    public static SeasonGroupedViewModel<MatchdayGroupViewModel> MergeCalendar(
        SeasonGroupedViewModel<MatchdayGroupViewModel>? results,
        SeasonGroupedViewModel<MatchdayGroupViewModel>? upcoming)
    {
        var seasonName = results?.SeasonName ?? upcoming?.SeasonName ?? string.Empty;
        var seasonSlug = results?.SeasonSlug ?? upcoming?.SeasonSlug ?? string.Empty;
        var result = new SeasonGroupedViewModel<MatchdayGroupViewModel>
        {
            SeasonName = seasonName,
            SeasonSlug = seasonSlug
        };

        var bySlug = new Dictionary<string, DivisionGroupViewModel<MatchdayGroupViewModel>>(StringComparer.OrdinalIgnoreCase);

        void Ingest(SeasonGroupedViewModel<MatchdayGroupViewModel>? source)
        {
            if (source?.Divisions == null) return;
            foreach (var div in source.Divisions)
            {
                if (!bySlug.TryGetValue(div.DivisionSlug, out var target))
                {
                    target = new DivisionGroupViewModel<MatchdayGroupViewModel>
                    {
                        DivisionName = div.DivisionName,
                        DivisionSlug = div.DivisionSlug,
                        Data = new List<MatchdayGroupViewModel>()
                    };
                    bySlug[div.DivisionSlug] = target;
                }

                MergeMatchdays(target, div.Data);
            }
        }

        // Preserve backend order: results divisions first (alphabetical), then any upcoming-only.
        Ingest(results);
        Ingest(upcoming);

        foreach (var div in bySlug.Values.OrderBy(d => d.DivisionName, StringComparer.OrdinalIgnoreCase))
        {
            div.Data = div.Data.OrderBy(m => m.Round).ToList();
            foreach (var md in div.Data)
                md.Matches = md.Matches.OrderBy(m => m.Kickoff).ToList();
            div.DefaultRound = ResolveInitialFecha(div);
            result.Divisions.Add(div);
        }

        return result;
    }

    public static int? ResolveInitialFecha(DivisionGroupViewModel<MatchdayGroupViewModel> div)
    {
        if (div.Data == null || div.Data.Count == 0) return null;

        var ordered = div.Data.OrderBy(md => md.Round).ToList();
        var lastPlayed = ordered
            .Where(HasFinishedMatch)
            .Select(md => md.Round)
            .DefaultIfEmpty(0)
            .Last();

        var open = ordered.Where(HasOpenMatch).ToList();
        if (open.Count == 0)
            return lastPlayed > 0 ? lastPlayed : ordered[0].Round;

        // Skip leftover earlier rounds (e.g. fecha 1 deferred to the end of the season).
        var nextAfterPlayed = open.FirstOrDefault(md => md.Round > lastPlayed);
        return (nextAfterPlayed ?? open[0]).Round;
    }

    public static int? ResolveLeagueNextFecha(SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar)
    {
        if (calendar?.Divisions == null || calendar.Divisions.Count == 0) return null;

        var resolved = calendar.Divisions
            .Select(ResolveInitialFecha)
            .Where(r => r.HasValue)
            .Select(r => r!.Value)
            .ToList();
        if (resolved.Count == 0) return null;

        return resolved
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .First()
            .Key;
    }

    private static bool HasOpenMatch(MatchdayGroupViewModel md) =>
        md.Matches != null && md.Matches.Any(m => !TeamDisplayHelper.IsFinished(m.Status));

    private static bool HasFinishedMatch(MatchdayGroupViewModel md) =>
        md.Matches != null && md.Matches.Any(m => TeamDisplayHelper.IsFinished(m.Status));

    /// <summary>
    /// Discrete label from real match statuses only. Returns null when not determinable.
    /// </summary>
    public static string? ResolveFechaStatus(MatchdayGroupViewModel? matchday)
    {
        if (matchday?.Matches == null || matchday.Matches.Count == 0) return null;

        var finished = matchday.Matches.Count(m => TeamDisplayHelper.IsFinished(m.Status));
        var total = matchday.Matches.Count;
        if (finished == total) return "Finalizada";
        if (finished > 0) return "En curso";
        return "Próxima";
    }

    private static void MergeMatchdays(
        DivisionGroupViewModel<MatchdayGroupViewModel> target,
        List<MatchdayGroupViewModel>? sourceDays)
    {
        if (sourceDays == null) return;
        foreach (var day in sourceDays)
        {
            var existing = target.Data.FirstOrDefault(d => d.Round == day.Round);
            if (existing == null)
            {
                target.Data.Add(new MatchdayGroupViewModel
                {
                    Round = day.Round,
                    Matches = day.Matches?.ToList() ?? new List<MatchViewModel>()
                });
                continue;
            }

            var known = new HashSet<Guid>(existing.Matches.Select(m => m.Id));
            foreach (var match in day.Matches ?? Enumerable.Empty<MatchViewModel>())
            {
                if (known.Add(match.Id))
                    existing.Matches.Add(match);
            }
        }
    }
}
