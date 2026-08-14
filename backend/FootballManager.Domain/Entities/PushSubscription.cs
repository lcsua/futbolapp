using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities;

public class PushSubscription : Entity
{
    public string Endpoint { get; private set; } = string.Empty;
    public string P256dh { get; private set; } = string.Empty;
    public string Auth { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime? LastUsedAt { get; private set; }
    public string? UserAgent { get; private set; }

    private readonly List<PushFollow> _follows = new();
    public virtual IReadOnlyCollection<PushFollow> Follows => _follows.AsReadOnly();

    protected PushSubscription() { }

    public PushSubscription(string endpoint, string p256dh, string auth, string? userAgent = null)
    {
        UpdateKeys(endpoint, p256dh, auth);
        UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim();
        IsActive = true;
    }

    public void UpdateKeys(string endpoint, string p256dh, string auth)
    {
        Endpoint = !string.IsNullOrWhiteSpace(endpoint)
            ? endpoint.Trim()
            : throw new ArgumentException("Endpoint is required.", nameof(endpoint));
        P256dh = !string.IsNullOrWhiteSpace(p256dh)
            ? p256dh.Trim()
            : throw new ArgumentException("P256dh is required.", nameof(p256dh));
        Auth = !string.IsNullOrWhiteSpace(auth)
            ? auth.Trim()
            : throw new ArgumentException("Auth is required.", nameof(auth));
        IsActive = true;
        UpdateTimestamp();
    }

    public void Touch()
    {
        LastUsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
