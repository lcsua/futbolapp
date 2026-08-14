using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Push;
using FootballManager.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FootballManager.Application.Services;

public sealed class PushNotificationService : IPushNotificationService
{
    private readonly IPushDispatchQueue _queue;
    private readonly IPushFollowQuery _followQuery;
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(
        IPushDispatchQueue queue,
        IPushFollowQuery followQuery,
        ILogger<PushNotificationService> logger)
    {
        _queue = queue;
        _followQuery = followQuery;
        _logger = logger;
    }

    public async Task NotifyResultUpdatedAsync(ResultUpdatedPushEvent evt, CancellationToken cancellationToken = default)
    {
        try
        {
            var teamIds = new[] { evt.HomeTeamId, evt.AwayTeamId };
            var teamFollowers = await _followQuery.GetActiveFollowersAsync(
                PushFollowScopeType.Team, teamIds, notifyResults: true, notifyFixture: false, cancellationToken);

            var teamNotified = new HashSet<Guid>();
            var title = $"{evt.HomeTeamName} {evt.HomeScore}-{evt.AwayScore} {evt.AwayTeamName}";
            var body = $"Ya está disponible el resultado de la Fecha {evt.RoundNumber}.";

            foreach (var teamId in teamIds)
            {
                var isHome = teamId == evt.HomeTeamId;
                var teamSlug = isHome ? evt.HomeTeamSlug : evt.AwayTeamSlug;
                var url = !string.IsNullOrWhiteSpace(teamSlug)
                    ? $"/ligas/{evt.LeagueSlug}/{teamSlug}"
                    : $"/ligas/{evt.LeagueSlug}/resultados";

                var targets = teamFollowers
                    .Where(f => f.ScopeId == teamId && teamNotified.Add(f.SubscriptionId))
                    .Select(f => new PushDispatchTarget(f.SubscriptionId, f.Endpoint, f.P256dh, f.Auth))
                    .ToList();

                if (targets.Count == 0) continue;

                await _queue.EnqueueAsync(new PushWorkItem
                {
                    EventType = PushNotificationEventType.ResultUpdated,
                    LeagueId = evt.LeagueId,
                    LeagueSlug = evt.LeagueSlug,
                    RoundNumber = evt.RoundNumber,
                    Payload = new PushPayloadDto(title, body, url),
                    Targets = targets
                }, cancellationToken);
            }

            await _queue.EnqueueAsync(new PushWorkItem
            {
                EventType = PushNotificationEventType.ResultUpdated,
                LeagueId = evt.LeagueId,
                LeagueSlug = evt.LeagueSlug,
                LeagueName = evt.LeagueName,
                RoundNumber = evt.RoundNumber,
                IsLeagueDigest = true,
                SuppressSubscriptionIds = teamNotified,
                Payload = new PushPayloadDto(
                    "Resultados actualizados",
                    $"Ya están disponibles los resultados de la Fecha {evt.RoundNumber} de {evt.LeagueName}.",
                    $"/ligas/{evt.LeagueSlug}/resultados")
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue ResultUpdated push for fixture {FixtureId}", evt.FixtureId);
        }
    }

    public async Task NotifyFixtureUpdatedAsync(FixtureUpdatedPushEvent evt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (evt.BulkAssign)
            {
                await _queue.EnqueueAsync(new PushWorkItem
                {
                    EventType = PushNotificationEventType.FixtureUpdated,
                    LeagueId = evt.LeagueId,
                    LeagueSlug = evt.LeagueSlug,
                    LeagueName = evt.LeagueName,
                    RoundNumber = evt.RoundNumber,
                    IsLeagueDigest = true,
                    Payload = new PushPayloadDto(
                        "Fixture actualizado",
                        $"Se actualizó el calendario de {evt.LeagueName}.",
                        $"/ligas/{evt.LeagueSlug}/fixture")
                }, cancellationToken);
                return;
            }

            var teamIds = new[] { evt.HomeTeamId, evt.AwayTeamId };
            var teamFollowers = await _followQuery.GetActiveFollowersAsync(
                PushFollowScopeType.Team, teamIds, notifyResults: false, notifyFixture: true, cancellationToken);

            var when = FormatWhen(evt.MatchDate, evt.KickoffTime);
            var field = string.IsNullOrWhiteSpace(evt.FieldName) ? null : evt.FieldName.Trim();
            var bodyBits = new List<string>();
            if (!string.IsNullOrWhiteSpace(when)) bodyBits.Add(when);
            if (!string.IsNullOrWhiteSpace(field)) bodyBits.Add(field);
            var detail = bodyBits.Count > 0
                ? string.Join(" · ", bodyBits)
                : "Se actualizó la información del partido.";

            var teamNotified = new HashSet<Guid>();
            foreach (var teamId in teamIds)
            {
                var isHome = teamId == evt.HomeTeamId;
                var teamSlug = isHome ? evt.HomeTeamSlug : evt.AwayTeamSlug;
                var url = !string.IsNullOrWhiteSpace(teamSlug)
                    ? $"/ligas/{evt.LeagueSlug}/{teamSlug}"
                    : $"/ligas/{evt.LeagueSlug}/fixture";

                var targets = teamFollowers
                    .Where(f => f.ScopeId == teamId && teamNotified.Add(f.SubscriptionId))
                    .Select(f => new PushDispatchTarget(f.SubscriptionId, f.Endpoint, f.P256dh, f.Auth))
                    .ToList();

                if (targets.Count == 0) continue;

                await _queue.EnqueueAsync(new PushWorkItem
                {
                    EventType = PushNotificationEventType.FixtureUpdated,
                    LeagueId = evt.LeagueId,
                    LeagueSlug = evt.LeagueSlug,
                    RoundNumber = evt.RoundNumber,
                    Payload = new PushPayloadDto(
                        "Cambio de partido",
                        $"{evt.HomeTeamName} vs {evt.AwayTeamName}\n{detail}",
                        url),
                    Targets = targets
                }, cancellationToken);
            }

            await _queue.EnqueueAsync(new PushWorkItem
            {
                EventType = PushNotificationEventType.FixtureUpdated,
                LeagueId = evt.LeagueId,
                LeagueSlug = evt.LeagueSlug,
                LeagueName = evt.LeagueName,
                RoundNumber = evt.RoundNumber,
                IsLeagueDigest = true,
                SuppressSubscriptionIds = teamNotified,
                Payload = new PushPayloadDto(
                    "Fixture actualizado",
                    $"Se actualizó un partido de {evt.LeagueName}.",
                    $"/ligas/{evt.LeagueSlug}/fixture")
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue FixtureUpdated push for fixture {FixtureId}", evt.FixtureId);
        }
    }

    private static string FormatWhen(DateOnly? date, TimeOnly? time)
    {
        if (date == null) return string.Empty;
        var d = date.Value.ToString("dddd d MMM", new CultureInfo("es-AR"));
        if (time == null) return d;
        return $"{d} {time.Value:HH:mm}";
    }
}

public sealed record PushFollowerRow(Guid SubscriptionId, Guid ScopeId, string Endpoint, string P256dh, string Auth);

public interface IPushFollowQuery
{
    Task<IReadOnlyList<PushFollowerRow>> GetActiveFollowersAsync(
        PushFollowScopeType scopeType,
        IEnumerable<Guid> scopeIds,
        bool notifyResults = false,
        bool notifyFixture = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PushFollowerRow>> GetActiveLeagueFollowersAsync(
        Guid leagueId,
        bool notifyResults = false,
        bool notifyFixture = false,
        CancellationToken cancellationToken = default);
}
