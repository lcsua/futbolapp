namespace PublicWeb.Seo;

public sealed class SeoOptions
{
    public const string SectionName = "Seo";

    /// <summary>Canonical public origin, e.g. https://miliga.com.ar (no trailing slash, no PathBase).</summary>
    public string PublicBaseUrl { get; set; } = "https://miliga.com.ar";

    /// <summary>
    /// When false (preview hosts such as develop.miliga.com.ar), emit noindex and a closed robots.txt
    /// so the environment is not indexed. Production stays true.
    /// </summary>
    public bool AllowIndexing { get; set; } = true;

    /// <summary>Default Open Graph image path relative to PublicBaseUrl.</summary>
    public string DefaultOgImagePath { get; set; } = "/branding/blue/icon-512.png";

    public string SiteName { get; set; } = "MiLiga";

    public string OrganizationName { get; set; } = "MiLiga";
}
