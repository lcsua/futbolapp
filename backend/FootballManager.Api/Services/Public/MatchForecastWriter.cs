using FootballManager.Application.Predictions;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Api.Services.Public;

/// <summary>
/// Persists the latest pre-kickoff forecast per match and model version.
/// Rows refresh while kickoff is still in the future; after kickoff they stay frozen.
/// </summary>
public sealed class MatchForecastWriter
{
    private readonly FootballManagerDbContext _db;
    private readonly ILogger<MatchForecastWriter> _logger;

    public MatchForecastWriter(FootballManagerDbContext db, ILogger<MatchForecastWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SaveAsync(
        string leagueSlug,
        string seasonSlug,
        string divisionSlug,
        DivisionOutlook outlook,
        CancellationToken cancellationToken)
    {
        if (!outlook.Published || outlook.NextRound.Count == 0)
            return;

        try
        {
            var matchIds = outlook.NextRound.Select(item => item.MatchId).ToList();
            var existing = await _db.MatchPredictionForecasts
                .Where(item => matchIds.Contains(item.MatchId) && item.ModelVersion == outlook.ModelVersion)
                .ToListAsync(cancellationToken);
            var byMatch = existing.ToDictionary(item => item.MatchId);
            var now = DateTime.UtcNow;

            foreach (var match in outlook.NextRound)
            {
                if (match.MatchId == Guid.Empty)
                    continue;
                if (MatchForecastTime.IsStarted(match.Kickoff, now))
                    continue;

                if (byMatch.TryGetValue(match.MatchId, out var row))
                {
                    row.RefreshPreKickoff(
                        match.HomeWin,
                        match.Draw,
                        match.AwayWin,
                        match.LikelyHomeGoals,
                        match.LikelyAwayGoals,
                        match.HomeLambda,
                        match.AwayLambda,
                        match.Kickoff,
                        now);
                    continue;
                }

                _db.MatchPredictionForecasts.Add(MatchPredictionForecast.Create(
                    leagueSlug,
                    seasonSlug,
                    divisionSlug,
                    match.MatchId,
                    match.Round,
                    match.Kickoff,
                    match.HomeTeamId,
                    match.AwayTeamId,
                    match.HomeWin,
                    match.Draw,
                    match.AwayWin,
                    match.LikelyHomeGoals,
                    match.LikelyAwayGoals,
                    match.HomeLambda,
                    match.AwayLambda,
                    outlook.ModelVersion,
                    now));
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudieron guardar los pronósticos de partido de {Division}", divisionSlug);
        }
    }
}

internal static class MatchForecastTime
{
    public static DateTime? ToUtc(DateTime? kickoff)
    {
        if (kickoff == null)
            return null;
        var value = kickoff.Value;
        if (value.Kind == DateTimeKind.Utc)
            return value;
        if (value.Kind == DateTimeKind.Local)
            return value.ToUniversalTime();
        // Unspecified kickoffs in MiLiga are wall-clock Argentina time.
        return TimeZoneInfo.ConvertTimeToUtc(value, ArgentinaZone());
    }

    public static bool IsStarted(DateTime? kickoff, DateTime utcNow)
    {
        var utc = ToUtc(kickoff);
        return utc != null && utc <= utcNow;
    }

    private static TimeZoneInfo ArgentinaZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }
}
