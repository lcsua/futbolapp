using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Api.Services.Public;

public sealed record MatchForecastScoreSummary(
    int Forecasts,
    int Scored,
    double ResultAccuracy,
    double ExactScoreAccuracy,
    double MeanAbsoluteGoalError,
    double BrierScore,
    double LogLoss);

/// <summary>
/// Internal scoring helpers over pre-kickoff forecasts.
/// The valid forecast for a finished match is the latest row with
/// CalculatedAt &lt;= Kickoff UTC (or any row if kickoff is unknown).
/// </summary>
public sealed class MatchForecastScorecard
{
    private readonly FootballManagerDbContext _db;

    public MatchForecastScorecard(FootballManagerDbContext db) => _db = db;

    public async Task<MatchForecastScoreSummary> SummarizeAsync(
        string? leagueSlug,
        string? seasonSlug,
        string? divisionSlug,
        CancellationToken cancellationToken)
    {
        var query = _db.MatchPredictionForecasts.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(leagueSlug))
            query = query.Where(item => item.LeagueSlug == leagueSlug);
        if (!string.IsNullOrWhiteSpace(seasonSlug))
            query = query.Where(item => item.SeasonSlug == seasonSlug);
        if (!string.IsNullOrWhiteSpace(divisionSlug))
            query = query.Where(item => item.DivisionSlug == divisionSlug);

        var forecasts = await query.ToListAsync(cancellationToken);
        var matchIds = forecasts.Select(item => item.MatchId).Distinct().ToList();
        var results = await _db.Results.AsNoTracking()
            .Where(item => matchIds.Contains(item.FixtureId))
            .ToListAsync(cancellationToken);
        var byFixture = results.ToDictionary(item => item.FixtureId);

        var scored = 0;
        var resultHits = 0;
        var exactHits = 0;
        var goalError = 0d;
        var brier = 0d;
        var logLoss = 0d;

        foreach (var group in forecasts.GroupBy(item => item.MatchId))
        {
            if (!byFixture.TryGetValue(group.Key, out var result))
                continue;
            var kickoff = group.Select(item => item.Kickoff).FirstOrDefault(item => item.HasValue);
            var forecast = SelectValid(group, kickoff);
            if (forecast == null)
                continue;

            scored++;
            var home = result.HomeTeamGoals;
            var away = result.AwayTeamGoals;
            var actual = home > away ? 0 : home == away ? 1 : 2;
            var probs = new[] { forecast.HomeWin, forecast.Draw, forecast.AwayWin };
            if (actual == Array.IndexOf(probs, probs.Max()))
                resultHits++;
            if (forecast.LikelyHomeGoals == home && forecast.LikelyAwayGoals == away)
                exactHits++;
            goalError += Math.Abs(forecast.ExpectedHomeGoals - home) + Math.Abs(forecast.ExpectedAwayGoals - away);
            for (var i = 0; i < 3; i++)
            {
                var target = i == actual ? 1d : 0d;
                brier += Math.Pow(probs[i] - target, 2);
            }
            var p = Math.Clamp(probs[actual], 1e-6, 1);
            logLoss += -Math.Log(p);
        }

        return new MatchForecastScoreSummary(
            forecasts.Count,
            scored,
            scored == 0 ? 0 : resultHits / (double)scored,
            scored == 0 ? 0 : exactHits / (double)scored,
            scored == 0 ? 0 : goalError / scored,
            scored == 0 ? 0 : brier / scored,
            scored == 0 ? 0 : logLoss / scored);
    }

    public static MatchPredictionForecast? SelectValid(
        IEnumerable<MatchPredictionForecast> forecasts,
        DateTime? kickoff)
    {
        var ordered = forecasts.OrderByDescending(item => item.CalculatedAt).ToList();
        if (kickoff == null)
            return ordered.FirstOrDefault();
        var utc = MatchForecastTime.ToUtc(kickoff);
        if (utc == null)
            return ordered.FirstOrDefault();
        // No fallback to a post-kickoff row: better to skip scoring than use a leak.
        return ordered.FirstOrDefault(item => item.CalculatedAt <= utc);
    }
}
