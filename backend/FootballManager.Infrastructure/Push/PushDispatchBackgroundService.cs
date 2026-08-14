using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Push;
using FootballManager.Application.Services;
using FootballManager.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FootballManager.Infrastructure.Push;

public sealed class PushDispatchBackgroundService : BackgroundService
{
    private readonly IPushDispatchQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WebPushOptions _options;
    private readonly ILogger<PushDispatchBackgroundService> _logger;
    private readonly ConcurrentDictionary<string, PendingDigest> _pendingDigests = new();

    public PushDispatchBackgroundService(
        IPushDispatchQueue queue,
        IServiceScopeFactory scopeFactory,
        IOptions<WebPushOptions> options,
        ILogger<PushDispatchBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var digestLoop = Task.Run(() => DigestLoopAsync(stoppingToken), stoppingToken);
        try
        {
            await foreach (var item in _queue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    if (item.IsLeagueDigest)
                        ScheduleDigest(item);
                    else
                        await SendTargetsAsync(item, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing push work item");
                }
            }
        }
        finally
        {
            try { await digestLoop; } catch { /* ignored */ }
        }
    }

    private void ScheduleDigest(PushWorkItem item)
    {
        if (item.LeagueId == null) return;
        var key = $"{item.EventType}:{item.LeagueId}:{item.RoundNumber?.ToString() ?? "all"}";
        _pendingDigests.AddOrUpdate(
            key,
            _ => new PendingDigest(item, DateTime.UtcNow.AddSeconds(Math.Max(5, _options.LeagueDigestDelaySeconds))),
            (_, existing) =>
            {
                foreach (var id in item.SuppressSubscriptionIds)
                    existing.Item.SuppressSubscriptionIds.Add(id);
                // Prefer latest payload text (still same round digest).
                return new PendingDigest(
                    new PushWorkItem
                    {
                        EventType = item.EventType,
                        LeagueId = item.LeagueId,
                        LeagueSlug = item.LeagueSlug ?? existing.Item.LeagueSlug,
                        LeagueName = item.LeagueName ?? existing.Item.LeagueName,
                        RoundNumber = item.RoundNumber ?? existing.Item.RoundNumber,
                        Payload = item.Payload,
                        IsLeagueDigest = true,
                        SuppressSubscriptionIds = existing.Item.SuppressSubscriptionIds
                    },
                    DateTime.UtcNow.AddSeconds(Math.Max(5, _options.LeagueDigestDelaySeconds)));
            });
    }

    private async Task DigestLoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var due = _pendingDigests
                    .Where(kv => kv.Value.ReadyAt <= now)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in due)
                {
                    if (!_pendingDigests.TryRemove(key, out var pending)) continue;
                    await FlushDigestAsync(pending.Item, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digest loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task FlushDigestAsync(PushWorkItem item, CancellationToken cancellationToken)
    {
        if (item.LeagueId == null) return;

        using var scope = _scopeFactory.CreateScope();
        var followQuery = scope.ServiceProvider.GetRequiredService<IPushFollowQuery>();
        var sender = scope.ServiceProvider.GetRequiredService<IWebPushSender>();

        var notifyResults = item.EventType == PushNotificationEventType.ResultUpdated;
        var notifyFixture = item.EventType == PushNotificationEventType.FixtureUpdated;

        var followers = await followQuery.GetActiveLeagueFollowersAsync(
            item.LeagueId.Value, notifyResults, notifyFixture, cancellationToken);

        foreach (var f in followers)
        {
            if (item.SuppressSubscriptionIds.Contains(f.SubscriptionId))
                continue;

            await sender.SendAsync(
                new PushDispatchTarget(f.SubscriptionId, f.Endpoint, f.P256dh, f.Auth),
                item.Payload,
                cancellationToken);
        }
    }

    private async Task SendTargetsAsync(PushWorkItem item, CancellationToken cancellationToken)
    {
        if (item.Targets.Count == 0) return;
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IWebPushSender>();
        foreach (var target in item.Targets)
            await sender.SendAsync(target, item.Payload, cancellationToken);
    }

    private sealed record PendingDigest(PushWorkItem Item, DateTime ReadyAt);
}
