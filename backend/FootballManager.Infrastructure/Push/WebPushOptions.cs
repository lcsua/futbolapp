namespace FootballManager.Infrastructure.Push;

public sealed class WebPushOptions
{
    public const string SectionName = "WebPush";

    public string Subject { get; set; } = "mailto:admin@miliga.com.ar";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    /// <summary>Public site origin used to absolutize notification URLs.</summary>
    public string PublicBaseUrl { get; set; } = "https://miliga.com.ar";
    public string DefaultIconPath { get; set; } = "/branding/blue/icon-192.png";
    public string DefaultBadgePath { get; set; } = "/branding/blue/icon-192.png";
    public int LeagueDigestDelaySeconds { get; set; } = 45;
}
