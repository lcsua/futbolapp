using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Push;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Push;

public sealed class PushSubscriptionService : IPushSubscriptionService
{
    private readonly FootballManagerDbContext _db;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ITeamRepository _teamRepository;

    public PushSubscriptionService(
        FootballManagerDbContext db,
        ILeagueRepository leagueRepository,
        ITeamRepository teamRepository)
    {
        _db = db;
        _leagueRepository = leagueRepository;
        _teamRepository = teamRepository;
    }

    public async Task UpsertSubscriptionAsync(string endpoint, string p256dh, string auth, string? userAgent, CancellationToken cancellationToken = default)
    {
        ValidateKeys(endpoint, p256dh, auth);
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint.Trim(), cancellationToken);
        if (existing == null)
        {
            await _db.PushSubscriptions.AddAsync(new PushSubscription(endpoint, p256dh, auth, userAgent), cancellationToken);
        }
        else
        {
            existing.UpdateKeys(endpoint, p256dh, auth);
            existing.Touch();
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task FollowAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        if (scopeId == Guid.Empty)
            throw new ArgumentException("ScopeId is required.");
        await EnsureScopeExistsAsync(scopeType, scopeId, cancellationToken);

        var sub = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint.Trim(), cancellationToken)
            ?? throw new InvalidOperationException("Push subscription not found. Call subscribe first.");

        sub.Touch();

        var existing = await _db.PushFollows.FirstOrDefaultAsync(
            f => f.PushSubscriptionId == sub.Id && f.ScopeType == scopeType && f.ScopeId == scopeId,
            cancellationToken);

        if (existing == null)
        {
            await _db.PushFollows.AddAsync(new PushFollow(sub, scopeType, scopeId), cancellationToken);
        }
        else
        {
            existing.EnableAllChannels();
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnfollowAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        var sub = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint.Trim(), cancellationToken);
        if (sub == null) return;

        var follow = await _db.PushFollows.FirstOrDefaultAsync(
            f => f.PushSubscriptionId == sub.Id && f.ScopeType == scopeType && f.ScopeId == scopeId,
            cancellationToken);
        if (follow == null) return;

        follow.SoftDelete();
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsFollowingAsync(string endpoint, PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || scopeId == Guid.Empty) return false;
        return await _db.PushFollows.AnyAsync(
            f => f.PushSubscription.Endpoint == endpoint.Trim()
                 && f.ScopeType == scopeType
                 && f.ScopeId == scopeId
                 && f.PushSubscription.IsActive,
            cancellationToken);
    }

    private async Task EnsureScopeExistsAsync(PushFollowScopeType scopeType, Guid scopeId, CancellationToken cancellationToken)
    {
        switch (scopeType)
        {
            case PushFollowScopeType.League:
                var league = await _leagueRepository.GetByIdAsync(scopeId, cancellationToken);
                if (league == null || !league.IsPublic)
                    throw new KeyNotFoundException("League not found or not public.");
                break;
            case PushFollowScopeType.Team:
                var team = await _teamRepository.GetByIdAsync(scopeId, cancellationToken);
                if (team == null)
                    throw new KeyNotFoundException("Team not found.");
                var teamLeague = await _leagueRepository.GetByIdAsync(team.LeagueId, cancellationToken);
                if (teamLeague == null || !teamLeague.IsPublic)
                    throw new KeyNotFoundException("Team league not found or not public.");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scopeType), "Unsupported scope type.");
        }
    }

    private static void ValidateKeys(string endpoint, string p256dh, string auth)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > 2048)
            throw new ArgumentException("Invalid endpoint.");
        if (string.IsNullOrWhiteSpace(p256dh) || p256dh.Length > 512)
            throw new ArgumentException("Invalid p256dh.");
        if (string.IsNullOrWhiteSpace(auth) || auth.Length > 512)
            throw new ArgumentException("Invalid auth.");
        if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Endpoint must be https.");
    }
}
