using FootballManager.Application.Predictions;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Api.Services.Public;

/// <summary>
/// Stores the probability series after a cache miss. A missing table or a write
/// error is logged and does not hide the prediction that was already calculated.
/// </summary>
public sealed class PredictionSnapshotWriter
{
    private readonly FootballManagerDbContext _db;
    private readonly ILogger<PredictionSnapshotWriter> _logger;

    public PredictionSnapshotWriter(FootballManagerDbContext db, ILogger<PredictionSnapshotWriter> logger)
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
        if (outlook.History is not { Available: true } history)
            return;

        try
        {
            var existing = await _db.PredictionSnapshots
                .Where(item => item.LeagueSlug == leagueSlug
                    && item.SeasonSlug == seasonSlug
                    && item.DivisionSlug == divisionSlug
                    && item.ModelVersion == outlook.ModelVersion)
                .ToListAsync(cancellationToken);
            var now = DateTime.UtcNow;
            foreach (var round in history.Rounds)
            {
                foreach (var team in round.Teams)
                {
                    var row = existing.FirstOrDefault(item =>
                        item.TeamId == team.TeamId
                        && item.CutoffRound == round.Round
                        && item.Reconstructed == round.Reconstructed);
                    if (row == null)
                    {
                        _db.PredictionSnapshots.Add(PredictionSnapshot.Create(
                            leagueSlug,
                            seasonSlug,
                            divisionSlug,
                            team.TeamId,
                            round.Round,
                            round.Reconstructed,
                            team.TitleProbability,
                            team.PodiumProbability,
                            team.ExpectedPosition,
                            team.ExpectedPoints,
                            outlook.ModelVersion,
                            now));
                    }
                    else if (!round.Reconstructed)
                    {
                        row.Refresh(team.TitleProbability, team.PodiumProbability, team.ExpectedPosition, team.ExpectedPoints, now);
                    }
                }
            }

            var liveRound = history.Rounds.LastOrDefault(item => !item.Reconstructed)?.Round;
            if (liveRound != null)
            {
                foreach (var stale in existing.Where(item => !item.Reconstructed && item.CutoffRound != liveRound))
                    _db.PredictionSnapshots.Remove(stale);
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "No se guardó el historial de predicciones de {League} {Division}.", leagueSlug, divisionSlug);
            _db.ChangeTracker.Clear();
        }
    }
}
