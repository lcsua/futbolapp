using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Services.Public;

/// <summary>
/// Turns standings plus recent results into the match-page preview.
/// Missing samples stay null: they are not treated as zero.
/// </summary>
public static class MatchPreviewBuilder
{
    public const int FormSlots = 5;
    public const int StreakSample = 10;

    public static MatchPreviewViewModel Build(
        MatchViewModel match,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings,
        TeamDetailViewModel? homeTeam,
        TeamDetailViewModel? awayTeam)
    {
        var division = DivisionRows(match, standings);
        var home = Snapshot(match.HomeTeam, homeTeam, RowFor(match.HomeTeam.Id, division, standings), match);
        var away = Snapshot(match.AwayTeam, awayTeam, RowFor(match.AwayTeam.Id, division, standings), match);
        var rows = CompareRows(home, away);
        var insights = new MatchInsightsService().Select(new MatchInsightInput
        {
            Home = home.Input,
            Away = away.Input,
            Division = division.Select(ToDivisionInput).ToList(),
            DivisionName = match.DivisionName
        });

        return new MatchPreviewViewModel
        {
            HomeName = match.HomeTeam.Name,
            AwayName = match.AwayTeam.Name,
            Home = home.Display,
            Away = away.Display,
            ShowPlace = home.Display.HasStanding || away.Display.HasStanding,
            ShowForm = home.Display.Form.Count > 0 || away.Display.Form.Count > 0,
            HasComparison = rows.Count > 0,
            Rows = rows,
            Insights = insights
        };
    }

    public static MatchCardPreview Card(
        MatchViewModel match,
        TeamDetailViewModel? homeTeam,
        TeamDetailViewModel? awayTeam)
    {
        var home = Snapshot(match.HomeTeam, homeTeam, null, match);
        var away = Snapshot(match.AwayTeam, awayTeam, null, match);
        return new MatchCardPreview
        {
            Home = home.Display,
            Away = away.Display,
            ShowPlace = home.Display.HasStanding || away.Display.HasStanding,
            ShowForm = home.Display.Form.Count > 0 || away.Display.Form.Count > 0
        };
    }

    /// <summary>Bar length relative to the larger side. Both zeros occupy nothing.</summary>
    public static (double Home, double Away) BarShares(int? home, int? away)
    {
        if (home is null && away is null) return (0, 0);
        if (home is null) return (0, away > 0 ? 1 : 0);
        if (away is null) return (home > 0 ? 1 : 0, 0);
        var max = Math.Max(home.Value, away.Value);
        if (max <= 0) return (0, 0);
        return (home.Value / (double)max, away.Value / (double)max);
    }

    private static List<MatchCompareRow> CompareRows(SideSnapshot home, SideSnapshot away)
    {
        var rows = new List<MatchCompareRow>();
        Add(rows, "Partidos jugados", Stat(home, s => s.Played), Stat(away, s => s.Played));
        Add(rows, "Puntos", Stat(home, s => s.Points), Stat(away, s => s.Points));
        Add(rows, "Victorias", Stat(home, s => s.Wins), Stat(away, s => s.Wins));
        Add(rows, "Empates", Stat(home, s => s.Draws), Stat(away, s => s.Draws));
        Add(rows, "Derrotas", Stat(home, s => s.Losses), Stat(away, s => s.Losses));
        Add(rows, "Goles a favor", Stat(home, s => s.GoalsFor), Stat(away, s => s.GoalsFor));
        Add(rows, "Goles en contra", Stat(home, s => s.GoalsAgainst), Stat(away, s => s.GoalsAgainst));
        Add(rows, "Puntos en los últimos 5 partidos", home.RecentPoints, away.RecentPoints);
        return rows;
    }

    private static void Add(List<MatchCompareRow> rows, string label, int? home, int? away)
    {
        if (home is null && away is null) return;
        var (homeShare, awayShare) = BarShares(home, away);
        rows.Add(new MatchCompareRow
        {
            Label = label,
            Home = home,
            Away = away,
            HomeShare = homeShare,
            AwayShare = awayShare
        });
    }

    private static int? Stat(SideSnapshot side, Func<MatchSideInput, int> pick) =>
        side.Input.HasStanding ? pick(side.Input) : null;

    private static SideSnapshot Snapshot(
        TeamViewModel team,
        TeamDetailViewModel? summary,
        StandingsRowViewModel? row,
        MatchViewModel focus)
    {
        var teamId = team.Id != Guid.Empty ? team.Id : summary?.Team.Id ?? Guid.Empty;
        var recent = ReadRecent(summary, teamId, focus);
        var standing = row != null
            ? new StandingNumbers(row.Position, row.Played, row.Points, row.Won, row.Drawn, row.Lost, row.GoalsFor, row.GoalsAgainst)
            : summary?.Standing is { } fromSummary
                ? new StandingNumbers(
                    fromSummary.Position,
                    fromSummary.Played,
                    fromSummary.Points,
                    fromSummary.Wins,
                    fromSummary.Draws,
                    fromSummary.Losses,
                    fromSummary.GoalsFor,
                    fromSummary.GoalsAgainst)
                : null;

        var input = new MatchSideInput
        {
            TeamId = teamId,
            Name = string.IsNullOrWhiteSpace(team.Name) ? summary?.Team.Name ?? string.Empty : team.Name,
            HasStanding = standing != null,
            Position = standing?.Position ?? 0,
            Played = standing?.Played ?? 0,
            Points = standing?.Points ?? 0,
            Wins = standing?.Wins ?? 0,
            Draws = standing?.Draws ?? 0,
            Losses = standing?.Losses ?? 0,
            GoalsFor = standing?.GoalsFor ?? 0,
            GoalsAgainst = standing?.GoalsAgainst ?? 0,
            RecentKnown = summary != null,
            Recent = recent
        };

        int? recentPoints = null;
        if (input.RecentKnown)
        {
            var window = recent.Take(FormSlots).ToList();
            if (window.Count > 0)
                recentPoints = window.Sum(PointsOf);
        }

        return new SideSnapshot
        {
            Input = input,
            Display = new TeamArrivalDisplay
            {
                HasStanding = input.HasStanding,
                Position = input.Position,
                Points = input.Points,
                FormKnown = input.RecentKnown,
                Form = recent.Take(FormSlots).Select(Letter).ToList()
            },
            RecentPoints = recentPoints
        };
    }

    private static List<RecentResultInput> ReadRecent(TeamDetailViewModel? summary, Guid teamId, MatchViewModel focus)
    {
        if (summary == null || teamId == Guid.Empty) return new List<RecentResultInput>();

        var results = new List<RecentResultInput>();
        foreach (var match in summary.LastResults)
        {
            if (results.Count >= StreakSample) break;
            if (focus.Id != Guid.Empty && match.Id == focus.Id) continue;
            if (focus.Kickoff != default && match.Kickoff != default && match.Kickoff > focus.Kickoff) continue;
            if (TeamDisplayHelper.FormLetter(match, teamId) is null) continue;

            var isHome = match.HomeTeam.Id == teamId;
            var goalsFor = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            var goalsAgainst = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;
            results.Add(new RecentResultInput { GoalsFor = goalsFor, GoalsAgainst = goalsAgainst });
        }

        return results;
    }

    private static List<StandingsRowViewModel> DivisionRows(
        MatchViewModel match,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings)
    {
        var divisions = standings?.Divisions ?? new List<DivisionGroupViewModel<StandingsRowViewModel>>();
        DivisionGroupViewModel<StandingsRowViewModel>? named = null;
        if (!string.IsNullOrWhiteSpace(match.DivisionName))
        {
            named = divisions.FirstOrDefault(d =>
                string.Equals(d.DivisionName, match.DivisionName, StringComparison.OrdinalIgnoreCase));
        }

        bool ContainsBoth(DivisionGroupViewModel<StandingsRowViewModel> division) =>
            division.Data.Any(r => r.Team.Id == match.HomeTeam.Id)
            && division.Data.Any(r => r.Team.Id == match.AwayTeam.Id);

        if (named != null && ContainsBoth(named)) return named.Data;
        var both = divisions.FirstOrDefault(ContainsBoth);
        if (both != null) return both.Data;
        if (named != null) return named.Data;
        return new List<StandingsRowViewModel>();
    }

    private static StandingsRowViewModel? RowFor(
        Guid teamId,
        IReadOnlyList<StandingsRowViewModel> division,
        SeasonGroupedViewModel<StandingsRowViewModel>? standings)
    {
        if (teamId == Guid.Empty) return null;
        var inDivision = division.FirstOrDefault(r => r.Team.Id == teamId);
        if (inDivision != null) return inDivision;
        return standings?.Divisions
            .SelectMany(d => d.Data)
            .FirstOrDefault(r => r.Team.Id == teamId);
    }

    private static DivisionStandingInput ToDivisionInput(StandingsRowViewModel row) => new()
    {
        TeamId = row.Team.Id,
        Name = row.Team.Name,
        Position = row.Position,
        Played = row.Played,
        Points = row.Points,
        GoalsFor = row.GoalsFor,
        GoalsAgainst = row.GoalsAgainst
    };

    private static char Letter(RecentResultInput result)
    {
        if (result.GoalsFor > result.GoalsAgainst) return 'G';
        if (result.GoalsFor < result.GoalsAgainst) return 'P';
        return 'E';
    }

    private static int PointsOf(RecentResultInput result)
    {
        if (result.GoalsFor > result.GoalsAgainst) return 3;
        if (result.GoalsFor == result.GoalsAgainst) return 1;
        return 0;
    }

    private sealed class SideSnapshot
    {
        public MatchSideInput Input { get; init; } = new();
        public TeamArrivalDisplay Display { get; init; } = new();
        public int? RecentPoints { get; init; }
    }

    private sealed record StandingNumbers(
        int Position,
        int Played,
        int Points,
        int Wins,
        int Draws,
        int Losses,
        int GoalsFor,
        int GoalsAgainst);
}
