using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities;

public class Advertisement : Entity
{
    public Guid LeagueId { get; private set; }
    public virtual League League { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;
    public string AdvertiserName { get; private set; } = string.Empty;
    public string? DesktopImageUrl { get; private set; }
    public string? MobileImageUrl { get; private set; }
    public string? TargetUrl { get; private set; }
    public AdvertisementSlot Slot { get; private set; }
    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }

    protected Advertisement() { }

    public Advertisement(
        League league,
        string name,
        string advertiserName,
        AdvertisementSlot slot,
        string? desktopImageUrl = null,
        string? mobileImageUrl = null,
        string? targetUrl = null,
        DateTime? startsAt = null,
        DateTime? endsAt = null,
        int priority = 0,
        bool isActive = true)
    {
        League = league ?? throw new ArgumentNullException(nameof(league));
        LeagueId = league.Id;
        Name = RequireText(name, nameof(name), "Advertisement name cannot be empty.");
        AdvertiserName = RequireText(advertiserName, nameof(advertiserName), "Advertiser name cannot be empty.");
        if (!Enum.IsDefined(slot))
            throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        DesktopImageUrl = NormalizeOptional(desktopImageUrl);
        MobileImageUrl = NormalizeOptional(mobileImageUrl);
        TargetUrl = NormalizeOptional(targetUrl);
        EnsureValidSchedule(startsAt, endsAt);
        StartsAt = startsAt;
        EndsAt = endsAt;
        Priority = EnsurePriority(priority);
        IsActive = isActive;
    }

    public void Update(
        string name,
        string advertiserName,
        AdvertisementSlot slot,
        string? targetUrl,
        DateTime? startsAt,
        DateTime? endsAt,
        int priority,
        bool isActive)
    {
        Name = RequireText(name, nameof(name), "Advertisement name cannot be empty.");
        AdvertiserName = RequireText(advertiserName, nameof(advertiserName), "Advertiser name cannot be empty.");
        if (!Enum.IsDefined(slot))
            throw new ArgumentOutOfRangeException(nameof(slot));
        Slot = slot;
        TargetUrl = NormalizeOptional(targetUrl);
        EnsureValidSchedule(startsAt, endsAt);
        StartsAt = startsAt;
        EndsAt = endsAt;
        Priority = EnsurePriority(priority);
        IsActive = isActive;
        UpdateTimestamp();
    }

    public void SetDesktopImage(string imageUrl)
    {
        DesktopImageUrl = RequireText(imageUrl, nameof(imageUrl), "Desktop image URL cannot be empty.");
        UpdateTimestamp();
    }

    public void SetMobileImage(string imageUrl)
    {
        MobileImageUrl = RequireText(imageUrl, nameof(imageUrl), "Mobile image URL cannot be empty.");
        UpdateTimestamp();
    }

    public void ClearDesktopImage()
    {
        DesktopImageUrl = null;
        UpdateTimestamp();
    }

    public void ClearMobileImage()
    {
        MobileImageUrl = null;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    private static string RequireText(string value, string paramName, string message)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException(message, paramName);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureValidSchedule(DateTime? startsAt, DateTime? endsAt)
    {
        if (startsAt.HasValue && endsAt.HasValue && endsAt.Value < startsAt.Value)
            throw new ArgumentException("EndsAt must be greater than or equal to StartsAt.");
    }

    private static int EnsurePriority(int priority)
    {
        if (priority < 0)
            throw new ArgumentOutOfRangeException(nameof(priority), "Priority must be greater than or equal to 0.");
        return priority;
    }
}
