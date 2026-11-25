using System.Globalization;
using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Services.Public;

/// <summary>
/// Team Resumen figures from the standing row and the matches already loaded
/// for that season. Table points stay as published (3 per win, 1 per draw).
/// </summary>
public static class TeamDashboardBuilder
{
    public const int ChartMatches = 10;
    public const int SpotlightPlaces = 3;
    public const int UpcomingRivals = 3;
    public const int PointsForWin = 3;

    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static TeamDashboard Build(TeamDetailViewModel? summary)
    {
        summary ??= new TeamDetailViewModel();
        var teamId = summary.Team?.Id ?? Guid.Empty;
        var teamName = string.IsNullOrWhiteSpace(summary.Team?.Name) ? "El equipo" : summary.Team!.Name.Trim();
        var played = ReadPlayed(summary, teamId);
        var finished = summary.Season is { IsActive: false };
        var scorers = BuildScorers(summary.Scorers, out var hasMore);

        return new TeamDashboard
        {
            Numbers = BuildNumbers(summary.Standing),
            Insights = BuildInsights(summary.Standing, played, teamId, teamName),
            Evolution = BuildEvolution(played),
            Scorers = scorers,
            HasMoreScorers = hasMore,
            Road = BuildRoad(summary, teamId, finished)
        };
    }

    private static TeamNumbersDisplay? BuildNumbers(StandingSummaryViewModel? standing)
    {
        if (standing == null || standing.Played <= 0)
            return standing == null ? null : new TeamNumbersDisplay { Played = 0, BalanceLabel = "Todavía no jugó en este campeonato." };

        var played = standing.Played;
        var possible = played * PointsForWin;
        return new TeamNumbersDisplay
        {
            Played = played,
            Wins = standing.Wins,
            Draws = standing.Draws,
            Losses = standing.Losses,
            GoalsFor = standing.GoalsFor,
            GoalsAgainst = standing.GoalsAgainst,
            WinPercent = Percent(standing.Wins, played),
            PointsPercent = possible > 0 ? Percent(standing.Points, possible) : null,
            GoalsForAverage = Average(standing.GoalsFor, played),
            GoalsAgainstAverage = Average(standing.GoalsAgainst, played),
            WinsLabel = Count(standing.Wins, "ganado", "ganados"),
            DrawsLabel = Count(standing.Draws, "empatado", "empatados"),
            LossesLabel = Count(standing.Losses, "perdido", "perdidos"),
            BalanceLabel = $"{Count(standing.Wins, "ganado", "ganados")}, {Count(standing.Draws, "empatado", "empatados")} y {Count(standing.Losses, "perdido", "perdidos")}"
        };
    }

    private static IReadOnlyList<MatchInsight> BuildInsights(
        StandingSummaryViewModel? standing,
        IReadOnlyList<PlayedMatch> played,
        Guid teamId,
        string teamName)
    {
        if (played.Count == 0 && standing == null)
            return Array.Empty<MatchInsight>();

        var recent = played
            .Take(ChartMatches)
            .Select(p => new RecentResultInput { GoalsFor = p.GoalsFor, GoalsAgainst = p.GoalsAgainst })
            .ToList();

        var side = new MatchSideInput
        {
            TeamId = teamId,
            Name = teamName,
            HasStanding = standing != null,
            Position = standing?.Position ?? 0,
            Played = standing?.Played ?? 0,
            Points = standing?.Points ?? 0,
            Wins = standing?.Wins ?? 0,
            Draws = standing?.Draws ?? 0,
            Losses = standing?.Losses ?? 0,
            GoalsFor = standing?.GoalsFor ?? 0,
            GoalsAgainst = standing?.GoalsAgainst ?? 0,
            RecentKnown = true,
            Recent = recent
        };

        return new MatchInsightsService(new MatchInsightThresholds { MaxInsights = 3 }).Select(new MatchInsightInput
        {
            Home = side,
            Away = new MatchSideInput()
        });
    }

    private static TeamEvolutionDisplay? BuildEvolution(IReadOnlyList<PlayedMatch> played)
    {
        var window = played.Take(ChartMatches).Reverse().ToList();
        if (window.Count == 0) return null;

        var scale = Math.Max(1, window.Max(m => Math.Max(m.GoalsFor, m.GoalsAgainst)));
        var ticks = scale <= 1
            ? new[] { scale, 0 }
            : new[] { scale, scale / 2, 0 };

        return new TeamEvolutionDisplay
        {
            Title = window.Count == 1 ? "Último partido" : $"Últimos {window.Count} partidos",
            ScaleMax = scale,
            Ticks = ticks,
            Matches = window.Select(m => new TeamEvolutionMatch
            {
                Label = m.Match.RoundNumber > 0
                    ? $"F{m.Match.RoundNumber}"
                    : TeamDisplayHelper.FormatDayMonth(m.Match.Kickoff),
                Detail = Detail(m),
                Path = MatchSlugHelper.AppRelative(m.Match),
                GoalsFor = m.GoalsFor,
                GoalsAgainst = m.GoalsAgainst,
                ForShare = 100.0 * m.GoalsFor / scale,
                AgainstShare = 100.0 * m.GoalsAgainst / scale
            }).ToList()
        };
    }

    private static IReadOnlyList<TeamScorerSpotlight> BuildScorers(
        List<TeamScorerViewModel>? scorers,
        out bool hasMore)
    {
        var ordered = (scorers ?? new List<TeamScorerViewModel>())
            .Where(s => s.Goals > 0 && !string.IsNullOrWhiteSpace(s.PlayerName))
            .OrderByDescending(s => s.Goals)
            .ThenBy(s => s.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shown = new List<TeamScorerSpotlight>();
        var rank = 0;
        var previousGoals = -1;
        for (var index = 0; index < ordered.Count; index++)
        {
            var scorer = ordered[index];
            if (scorer.Goals != previousGoals)
            {
                rank = index + 1;
                previousGoals = scorer.Goals;
            }

            if (rank > SpotlightPlaces) break;

            shown.Add(new TeamScorerSpotlight
            {
                Rank = rank,
                Name = scorer.PlayerName.Trim(),
                Goals = scorer.Goals,
                GoalsLabel = scorer.Goals == 1 ? "1 gol" : $"{scorer.Goals} goles",
                PhotoUrl = PublicPhoto(scorer.PhotoUrl),
                Initials = TeamDisplayHelper.GetInitials(scorer.PlayerName)
            });
        }

        hasMore = ordered.Count > shown.Count;
        return shown;
    }

    private static TeamRoadDisplay? BuildRoad(TeamDetailViewModel summary, Guid teamId, bool finished)
    {
        var standing = summary.Standing;
        var pending = summary.NextMatchesTotal > 0
            ? summary.NextMatchesTotal
            : (summary.NextMatches?.Count ?? 0);

        if (finished)
        {
            if (standing == null)
            {
                return new TeamRoadDisplay
                {
                    Finished = true,
                    Headline = "Campeonato finalizado"
                };
            }

            var diff = standing.GoalDifference;
            var diffText = diff > 0 ? $"+{diff}" : diff.ToString(EsAr);
            var division = string.IsNullOrWhiteSpace(standing.DivisionName) ? null : standing.DivisionName.Trim();
            return new TeamRoadDisplay
            {
                Finished = true,
                Headline = TeamDisplayHelper.FormatPlaceCompact(standing.Position, standing.Points),
                Detail = string.Join(" · ", new[]
                {
                    division,
                    Count(standing.Played, "partido", "partidos"),
                    $"{diffText} de diferencia"
                }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Extra = $"{Count(standing.Wins, "victoria", "victorias")}, {Count(standing.Draws, "empate", "empates")} y {Count(standing.Losses, "derrota", "derrotas")}"
            };
        }

        if (standing == null && pending <= 0)
            return null;

        var rivals = pending > 0 ? ReadRivals(summary, teamId) : Array.Empty<TeamUpcomingRival>();
        string? extra = null;
        if (standing != null)
        {
            var max = standing.Points + pending * PointsForWin;
            extra = pending > 0
                ? $"Máximo alcanzable: {max} pts"
                : $"Lleva {Count(standing.Points, "punto", "puntos")}.";
        }

        return new TeamRoadDisplay
        {
            Finished = false,
            Headline = pending > 0
                ? Count(pending, "partido", "partidos")
                : "No quedan partidos",
            Detail = pending > 0
                ? $"{Count(pending * PointsForWin, "punto", "puntos")} en juego"
                : null,
            Extra = extra,
            Rivals = rivals
        };
    }

    private static IReadOnlyList<TeamUpcomingRival> ReadRivals(TeamDetailViewModel summary, Guid teamId)
    {
        var rivals = new List<TeamUpcomingRival>();
        foreach (var match in summary.NextMatches ?? new List<MatchViewModel>())
        {
            if (rivals.Count >= UpcomingRivals) break;
            var opponent = Opponent(match, teamId);
            if (opponent == null) continue;
            rivals.Add(new TeamUpcomingRival
            {
                Team = opponent,
                Match = match,
                When = When(match)
            });
        }

        return rivals;
    }

    private static List<PlayedMatch> ReadPlayed(TeamDetailViewModel summary, Guid teamId)
    {
        var played = new List<PlayedMatch>();
        foreach (var match in summary.LastResults ?? new List<MatchViewModel>())
        {
            if (TeamDisplayHelper.FormLetter(match, teamId) is not { } letter) continue;
            var isHome = match.HomeTeam?.Id == teamId;
            var goalsFor = isHome ? match.HomeScore!.Value : match.AwayScore!.Value;
            var goalsAgainst = isHome ? match.AwayScore!.Value : match.HomeScore!.Value;
            var rival = isHome ? match.AwayTeam : match.HomeTeam;
            played.Add(new PlayedMatch(match, goalsFor, goalsAgainst, letter, rival?.Name));
        }

        return played;
    }

    private static TeamViewModel? Opponent(MatchViewModel match, Guid teamId)
    {
        if (teamId == Guid.Empty) return null;
        if (match.HomeTeam?.Id == teamId) return match.AwayTeam;
        if (match.AwayTeam?.Id == teamId) return match.HomeTeam;
        return null;
    }

    private static string When(MatchViewModel match)
    {
        if (match.Kickoff != default)
            return TeamDisplayHelper.FormatWeekdayDayMonth(match.Kickoff);
        if (match.RoundNumber > 0)
            return $"Fecha {match.RoundNumber}";
        return "Fecha a confirmar";
    }

    private static string Detail(PlayedMatch played)
    {
        var word = played.Letter switch
        {
            'G' => "Victoria",
            'P' => "Derrota",
            _ => "Empate"
        };
        var rival = string.IsNullOrWhiteSpace(played.Rival) ? "el rival" : played.Rival.Trim();
        var bits = new List<string>();
        if (played.Match.RoundNumber > 0)
            bits.Add($"Fecha {played.Match.RoundNumber}");
        if (played.Match.Kickoff != default)
            bits.Add(TeamDisplayHelper.FormatShortDate(played.Match.Kickoff));
        bits.Add($"contra {rival}");
        bits.Add($"{word} {played.GoalsFor} — {played.GoalsAgainst}");
        return string.Join(" · ", bits);
    }

    private static int Percent(int part, int whole) =>
        (int)Math.Round(100.0 * part / whole, MidpointRounding.AwayFromZero);

    private static string Average(int goals, int played) =>
        (goals / (double)played).ToString("0.0", EsAr);

    private static string Count(int value, string singular, string plural) =>
        value == 1 ? $"1 {singular}" : $"{value} {plural}";

    /// <summary>Only a public image address. Data URLs and other schemes stay off the page.</summary>
    public static string? PublicPhoto(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        if (trimmed.StartsWith("/", StringComparison.Ordinal) && !trimmed.StartsWith("//", StringComparison.Ordinal))
            return trimmed;
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
            return trimmed;
        return null;
    }

    private sealed record PlayedMatch(MatchViewModel Match, int GoalsFor, int GoalsAgainst, char Letter, string? Rival);
}
