using FootballManager.Application.Predictions;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Api.Services.Public;

/// <summary>
/// Persists the first pre-kickoff forecast per match and model version.
/// Later cache refreshes never overwrite an existing row, so scoring can pick
/// the last forecast calculated before the match kickoff.
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
                .Select(item => item.MatchId)
                .ToListAsync(cancellationToken);
            var known = existing.ToHashSet();
            var now = DateTime.UtcNow;
            foreach (var match in outlook.NextRound)
            {
                if (match.MatchId == Guid.Empty || known.Contains(match.MatchId))
                    continue;
                if (match.Kickoff is DateTime kickoff && kickoff.ToUniversalTime() <= now)
                    continue;

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
