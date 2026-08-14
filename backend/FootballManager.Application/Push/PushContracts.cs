using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Push;

public sealed record PushPayloadDto(
    string Title,
    string Body,
    string Url,
    string? Icon = null,
    string? Badge = null);

public sealed record PushDispatchTarget(
    Guid SubscriptionId,
    string Endpoint,
    string P256dh,
    string Auth);

public sealed class ResultUpdatedPushEvent
{
    public Guid LeagueId { get; init; }
    public string LeagueSlug { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public Guid FixtureId { get; init; }
    public int RoundNumber { get; init; }
    public Guid HomeTeamId { get; init; }
    public Guid AwayTeamId { get; init; }
    public string HomeTeamName { get; init; } = string.Empty;
    public string AwayTeamName { get; init; } = string.Empty;
    public string? HomeTeamSlug { get; init; }
    public string? AwayTeamSlug { get; init; }
    public int HomeScore { get; init; }
    public int AwayScore { get; init; }
}

public sealed class FixtureUpdatedPushEvent
{
    public Guid LeagueId { get; init; }
    public string LeagueSlug { get; init; } = string.Empty;
    public string LeagueName { get; init; } = string.Empty;
    public Guid FixtureId { get; init; }
    public int RoundNumber { get; init; }
    public Guid HomeTeamId { get; init; }
    public Guid AwayTeamId { get; init; }
    public string HomeTeamName { get; init; } = string.Empty;
    public string AwayTeamName { get; init; } = string.Empty;
    public string? HomeTeamSlug { get; init; }
    public string? AwayTeamSlug { get; init; }
    public DateOnly? MatchDate { get; init; }
    public TimeOnly? KickoffTime { get; init; }
    public string? FieldName { get; init; }
    public bool BulkAssign { get; init; }
}

public interface IPushNotificationService
{
    Task NotifyResultUpdatedAsync(ResultUpdatedPushEvent evt, CancellationToken cancellationToken = default);
    Task NotifyFixtureUpdatedAsync(FixtureUpdatedPushEvent evt, CancellationToken cancellationToken = default);
}

public interface IPushSubscriptionService
{
    Task UpsertSubscriptionAsync(string endpoint, string p256dh, string auth, string? userAgent, CancellationToken cancellationToken = default);
    Task FollowAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default);
    Task UnfollowAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default);
}

public interface IPushDispatchQueue
{
    ValueTask EnqueueAsync(PushWorkItem item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PushWorkItem> ReadAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Immediate sends include <see cref="Targets"/>. League digests leave Targets empty;
/// the worker resolves league followers after debounce and skips SuppressSubscriptionIds.
/// </summary>
public sealed class PushWorkItem
{
    public PushNotificationEventType EventType { get; init; }
    public Guid? LeagueId { get; init; }
    public string? LeagueSlug { get; init; }
    public string? LeagueName { get; init; }
    public int? RoundNumber { get; init; }
    public PushPayloadDto Payload { get; init; } = new("", "", "/");
    public bool IsLeagueDigest { get; init; }
    public IReadOnlyList<PushDispatchTarget> Targets { get; init; } = Array.Empty<PushDispatchTarget>();
    public HashSet<Guid> SuppressSubscriptionIds { get; init; } = new();
}
