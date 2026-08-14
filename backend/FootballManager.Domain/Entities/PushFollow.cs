using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities;

public class PushFollow : Entity
{
    public Guid PushSubscriptionId { get; private set; }
    public virtual PushSubscription PushSubscription { get; private set; } = null!;

    public PushFollowScopeType ScopeType { get; private set; }
    public Guid ScopeId { get; private set; }

    public bool NotifyResults { get; private set; } = true;
    public bool NotifyFixture { get; private set; } = true;
    public bool NotifyStandings { get; private set; } = true;
    public bool NotifyNews { get; private set; } = true;

    protected PushFollow() { }

    public PushFollow(PushSubscription subscription, PushFollowScopeType scopeType, Guid scopeId)
    {
        PushSubscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
        PushSubscriptionId = subscription.Id;
        if (scopeId == Guid.Empty)
            throw new ArgumentException("ScopeId is required.", nameof(scopeId));
        ScopeType = scopeType;
        ScopeId = scopeId;
    }

    public void EnableAllChannels()
    {
        NotifyResults = true;
        NotifyFixture = true;
        NotifyStandings = true;
        NotifyNews = true;
        UpdateTimestamp();
    }
}
