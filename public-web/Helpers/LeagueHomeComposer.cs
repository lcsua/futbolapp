using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

/// <summary>
/// Builds the league home / hero surface from public data already loaded
/// (calendar + standings). Does not call the backend.
/// </summary>
public static class LeagueHomeComposer
{
    public const int MatchPreviewCount = 3;
    public const int StandingsPreviewCount = 5;

    public static LeagueHomeViewModel Compose(
        LeagueViewModel league,
        string seasonName,
        string seasonSlug,
        IReadOnlyList<DivisionViewModel> divisions,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings,
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar,
        string? selectedDivisionSlug)
    {
        var orderedDivisions = ResolveDivisionOrder(divisions, standings, calendar);
        var leagueRound = FixtureCalendarHelper.ResolveLeagueNextFecha(calendar);
        var panels = orderedDivisions
            .Select(div => ComposePanel(div, standings, calendar, leagueRound))
            .ToList();

        var selected = panels.FirstOrDefault(p =>
                            !string.IsNullOrWhiteSpace(selectedDivisionSlug)
                            && string.Equals(p.DivisionSlug, selectedDivisionSlug, StringComparison.OrdinalIgnoreCase))
                       ?? panels.FirstOrDefault();

        var nextFecha = ResolveHomeNextFecha(calendar);
        return new LeagueHomeViewModel
        {
            League = league,
            SeasonName = seasonName,
            SeasonSlug = seasonSlug,
            Divisions = orderedDivisions.ToList(),
            DivisionLeaders = ComposeLeaders(standings),
            NextFecha = nextFecha,
            Stats = BuildHeroStats(orderedDivisions.Count, CountUniqueTeams(standings), calendar),
            DivisionPanels = panels,
            SelectedDivisionSlug = selected?.DivisionSlug ?? string.Empty
        };
    }

    public static LeagueHeroStatsViewModel BuildHeroStats(
        int divisionCount,
        int? teamCount,
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar)
    {
        var round = FixtureCalendarHelper.ResolveLeagueNextFecha(calendar);
        return new LeagueHeroStatsViewModel
        {
            DivisionCount = divisionCount > 0 ? divisionCount : null,
            TeamCount = teamCount is > 0 ? teamCount : null,
            CurrentRound = round,
            CurrentRoundStatus = ResolveLeagueFechaStatus(calendar, round)
        };
    }

    public static int? CountUniqueTeams(SeasonGroupedViewModel<StandingsRowViewModel>? standings)
    {
        if (standings?.Divisions == null) return null;

        var seenIds = new HashSet<Guid>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var count = 0;

        foreach (var row in standings.Divisions.SelectMany(d => d.Data ?? new List<StandingsRowViewModel>()))
        {
            var team = row.Team;
            if (team == null) continue;

            if (team.Id != Guid.Empty)
            {
                if (!seenIds.Add(team.Id)) continue;
            }
            else
            {
                var key = !string.IsNullOrWhiteSpace(team.Slug) ? team.Slug : team.Name;
                if (string.IsNullOrWhiteSpace(key) || !seenKeys.Add(key)) continue;
            }

            count++;
        }

        return count > 0 ? count : null;
    }

    public static string CssId(string prefix, string? slug)
    {
        var safe = new string((slug ?? string.Empty)
            .Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            .ToArray());
        if (string.IsNullOrEmpty(safe)) safe = "x";
        if (char.IsAsciiDigit(safe[0])) safe = "d" + safe;
        return prefix + safe;
    }

    public static LeagueHomeNextFechaViewModel? ResolveHomeNextFecha(
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar)
    {
        if (calendar?.Divisions == null) return null;

        var round = FixtureCalendarHelper.ResolveLeagueNextFecha(calendar);
        if (!round.HasValue) return null;

        var matches = MatchesForRound(calendar, round.Value);
        if (matches.Count == 0) return null;

        return new LeagueHomeNextFechaViewModel
        {
            Round = round.Value,
            DisplayDate = MajorityDate(matches),
            MatchCount = matches.Count
        };
    }

    private static LeagueHomeDivisionPanelViewModel ComposePanel(
        DivisionViewModel div,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings,
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar,
        int? leagueRound)
    {
        var calDiv = FindDivision(calendar, div.Slug);
        var standDiv = FindDivision(standings, div.Slug);
        var round = calDiv?.DefaultRound ?? leagueRound;
        var matchday = round.HasValue
            ? calDiv?.Data?.FirstOrDefault(md => md.Round == round.Value)
            : null;
        var matches = matchday?.Matches ?? new List<MatchViewModel>();
        var rows = (standDiv?.Data ?? new List<StandingsRowViewModel>())
            .OrderBy(r => r.Position)
            .ThenByDescending(r => r.Points)
            .ToList();

        return new LeagueHomeDivisionPanelViewModel
        {
            DivisionName = !string.IsNullOrWhiteSpace(div.Name) ? div.Name : (calDiv?.DivisionName ?? standDiv?.DivisionName ?? div.Slug),
            DivisionSlug = div.Slug,
            Round = round,
            DisplayDate = MajorityDate(matches),
            FechaStatus = FixtureCalendarHelper.ResolveFechaStatus(matchday),
            Matches = matches.Take(MatchPreviewCount).ToList(),
            MatchCount = matches.Count,
            StandingsPreview = rows.Take(StandingsPreviewCount).ToList(),
            Teams = rows
                .Select(r => r.Team)
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.Name))
                .ToList()
        };
    }

    private static List<LeagueHomeDivisionLeaderViewModel> ComposeLeaders(
        SeasonGroupedViewModel<StandingsRowViewModel>? standings)
    {
        if (standings?.Divisions == null) return new List<LeagueHomeDivisionLeaderViewModel>();

        return standings.Divisions
            .Select(d =>
            {
                var leader = (d.Data ?? new List<StandingsRowViewModel>())
                    .OrderBy(r => r.Position)
                    .ThenByDescending(r => r.Points)
                    .FirstOrDefault();
                if (leader?.Team == null || string.IsNullOrWhiteSpace(leader.Team.Name))
                    return null;

                return new LeagueHomeDivisionLeaderViewModel
                {
                    DivisionName = d.DivisionName,
                    DivisionSlug = d.DivisionSlug,
                    Team = leader.Team,
                    Points = leader.Points
                };
            })
            .Where(x => x != null)
            .Cast<LeagueHomeDivisionLeaderViewModel>()
            .ToList();
    }

    private static IReadOnlyList<DivisionViewModel> ResolveDivisionOrder(
        IReadOnlyList<DivisionViewModel> divisions,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings,
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar)
    {
        if (divisions.Count > 0) return divisions;

        var fromData = new List<DivisionViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? name, string? slug)
        {
            if (string.IsNullOrWhiteSpace(slug) || !seen.Add(slug)) return;
            fromData.Add(new DivisionViewModel
            {
                Name = string.IsNullOrWhiteSpace(name) ? slug : name,
                Slug = slug
            });
        }

        if (standings?.Divisions != null)
        {
            foreach (var d in standings.Divisions)
                Add(d.DivisionName, d.DivisionSlug);
        }

        if (calendar?.Divisions != null)
        {
            foreach (var d in calendar.Divisions)
                Add(d.DivisionName, d.DivisionSlug);
        }

        return fromData;
    }

    private static DivisionGroupViewModel<T>? FindDivision<T>(
        SeasonGroupedViewModel<T>? grouped,
        string slug)
    {
        if (grouped?.Divisions == null || string.IsNullOrWhiteSpace(slug)) return null;
        return grouped.Divisions.FirstOrDefault(d =>
            string.Equals(d.DivisionSlug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveLeagueFechaStatus(
        SeasonGroupedViewModel<MatchdayGroupViewModel>? calendar,
        int? round)
    {
        if (calendar == null || !round.HasValue) return null;
        var matches = MatchesForRound(calendar, round.Value);
        if (matches.Count == 0) return null;
        return FixtureCalendarHelper.ResolveFechaStatus(new MatchdayGroupViewModel
        {
            Round = round.Value,
            Matches = matches
        });
    }

    private static List<MatchViewModel> MatchesForRound(
        SeasonGroupedViewModel<MatchdayGroupViewModel> calendar,
        int round)
    {
        return calendar.Divisions
            .SelectMany(d => d.Data ?? new List<MatchdayGroupViewModel>())
            .Where(md => md.Round == round)
            .SelectMany(md => md.Matches ?? new List<MatchViewModel>())
            .ToList();
    }

    private static DateTime? MajorityDate(IReadOnlyList<MatchViewModel> matches)
    {
        return matches
            .Where(m => m.Kickoff != default)
            .GroupBy(m => m.Kickoff.Date)
            .OrderByDescending(g => g.Count())
            .Select(g => (DateTime?)g.Key)
            .FirstOrDefault();
    }
}
