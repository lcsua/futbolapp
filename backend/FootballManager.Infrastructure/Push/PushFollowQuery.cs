using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Push;
using FootballManager.Application.Services;
using FootballManager.Domain.Enums;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Push;

public sealed class PushFollowQuery : IPushFollowQuery
{
    private readonly FootballManagerDbContext _db;

    public PushFollowQuery(FootballManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PushFollowerRow>> GetActiveFollowersAsync(
        PushFollowScopeType scopeType,
        IEnumerable<Guid> scopeIds,
        bool notifyResults = false,
        bool notifyFixture = false,
        CancellationToken cancellationToken = default)
    {
        var ids = scopeIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<PushFollowerRow>();

        var query = _db.PushFollows
            .AsNoTracking()
            .Where(f => f.ScopeType == scopeType && ids.Contains(f.ScopeId))
            .Where(f => f.PushSubscription.IsActive);

        if (notifyResults)
            query = query.Where(f => f.NotifyResults);
        if (notifyFixture)
            query = query.Where(f => f.NotifyFixture);

        return await query
            .Select(f => new PushFollowerRow(
                f.PushSubscriptionId,
                f.ScopeId,
                f.PushSubscription.Endpoint,
                f.PushSubscription.P256dh,
                f.PushSubscription.Auth))
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<PushFollowerRow>> GetActiveLeagueFollowersAsync(
        Guid leagueId,
        bool notifyResults = false,
        bool notifyFixture = false,
        CancellationToken cancellationToken = default)
        => GetActiveFollowersAsync(PushFollowScopeType.League, new[] { leagueId }, notifyResults, notifyFixture, cancellationToken);
}
