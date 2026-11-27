using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Services.Public;

/// <summary>
/// Loads the existing public standings and team summaries (already cached)
/// and builds the scheduled-match preview. Finished matches are left untouched.
/// </summary>
public sealed class MatchPreviewService
{
    public const int RecentFetchSize = 10;

    private readonly LeaguePublicService _leagues;
    private readonly TeamPublicService _teams;

    public MatchPreviewService(LeaguePublicService leagues, TeamPublicService teams)
    {
        _leagues = leagues;
        _teams = teams;
    }

    public async Task<MatchPreviewViewModel?> BuildAsync(MatchViewModel match)
    {
        if (!TeamDisplayHelper.IsScheduled(match.Status))
            return null;
        if (string.IsNullOrWhiteSpace(match.LeagueSlug))
            return MatchPreviewBuilder.Build(match, null, null, null);

        var homeTask = LoadTeamAsync(match.LeagueSlug, match.HomeTeam.Slug, match.SeasonSlug);
        var awayTask = LoadTeamAsync(match.LeagueSlug, match.AwayTeam.Slug, match.SeasonSlug);
        var tableTask = _leagues.GetStandingsAsync(match.LeagueSlug, match.SeasonSlug, null);
        var oddsTask = FindOddsAsync(match);
        await Task.WhenAll(homeTask, awayTask, tableTask, oddsTask);

        var preview = MatchPreviewBuilder.Build(match, await tableTask, await homeTask, await awayTask);
        var odds = await oddsTask;
        if (odds == null)
            return preview;

        return new MatchPreviewViewModel
        {
            HomeName = preview.HomeName,
            AwayName = preview.AwayName,
            Home = preview.Home,
            Away = preview.Away,
            ShowPlace = preview.ShowPlace,
            ShowForm = preview.ShowForm,
            HasComparison = preview.HasComparison,
            Rows = preview.Rows,
            Insights = preview.Insights,
            MatchOdds = odds
        };
    }

    private async Task<PredictionMatchViewModel?> FindOddsAsync(MatchViewModel match)
    {
        if (string.IsNullOrWhiteSpace(match.LeagueSlug) || match.Id == Guid.Empty)
            return null;

        try
        {
            var page = await _leagues.GetPredictionsAsync(match.LeagueSlug, match.SeasonSlug, null);
            if (page == null)
                return null;

            foreach (var division in page.Divisions)
            {
                var hit = division.NextRound.FirstOrDefault(item => item.MatchId == match.Id);
                if (hit != null)
                    return hit;
            }
        }
        catch
        {
            // Preview still works without odds.
        }

        return null;
    }

    public async Task<MatchCardPreview> BuildCardAsync(TeamDetailViewModel current, MatchViewModel match)
    {
        var leagueSlug = current.League?.Slug ?? match.LeagueSlug;
        var seasonSlug = current.Season?.Slug ?? match.SeasonSlug;
        var homeTask = ResolveAsync(current, match.HomeTeam, leagueSlug, seasonSlug);
        var awayTask = ResolveAsync(current, match.AwayTeam, leagueSlug, seasonSlug);
        await Task.WhenAll(homeTask, awayTask);
        return MatchPreviewBuilder.Card(match, await homeTask, await awayTask);
    }

    private async Task<TeamDetailViewModel?> ResolveAsync(
        TeamDetailViewModel current,
        TeamViewModel team,
        string? leagueSlug,
        string? seasonSlug)
    {
        if (SameTeam(current.Team, team))
            return current;
        return await LoadTeamAsync(leagueSlug, team.Slug, seasonSlug);
    }

    private Task<TeamDetailViewModel?> LoadTeamAsync(string? leagueSlug, string? teamSlug, string? seasonSlug)
    {
        if (string.IsNullOrWhiteSpace(leagueSlug) || string.IsNullOrWhiteSpace(teamSlug))
            return Task.FromResult<TeamDetailViewModel?>(null);

        return _teams.GetTeamSummaryAsync(
            leagueSlug,
            teamSlug,
            seasonSlug,
            nextPage: 1,
            resultsPage: 1,
            pageSize: RecentFetchSize);
    }

    private static bool SameTeam(TeamViewModel current, TeamViewModel other)
    {
        if (current.Id != Guid.Empty && current.Id == other.Id) return true;
        return !string.IsNullOrWhiteSpace(current.Slug)
            && string.Equals(current.Slug, other.Slug, StringComparison.OrdinalIgnoreCase);
    }
}
