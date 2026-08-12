using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Seo;

public static class SeoViewExtensions
{
    public static void ApplySeo(this Controller controller, SeoPageModel page)
    {
        controller.ViewData["Title"] = page.Title;
        controller.ViewData["Description"] = page.Description;
        controller.ViewData["CanonicalPath"] = page.CanonicalPath;
        controller.ViewData["OgImage"] = page.OgImage;
        controller.ViewData["OgType"] = page.OgType;
        controller.ViewData["NoIndex"] = page.NoIndex;
        if (!string.IsNullOrWhiteSpace(page.H1))
            controller.ViewBag.SeoH1 = page.H1;
        controller.ViewBag.SeoBreadcrumbs = page.Breadcrumbs;
    }

    public static void ApplySeo(this IDictionary<string, object?> viewData, dynamic viewBag, SeoPageModel page)
    {
        viewData["Title"] = page.Title;
        viewData["Description"] = page.Description;
        viewData["CanonicalPath"] = page.CanonicalPath;
        viewData["OgImage"] = page.OgImage;
        viewData["OgType"] = page.OgType;
        viewData["NoIndex"] = page.NoIndex;
        if (!string.IsNullOrWhiteSpace(page.H1))
            viewBag.SeoH1 = page.H1;
        viewBag.SeoBreadcrumbs = page.Breadcrumbs;
    }
}

public static class SeoJsonLd
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = null
    };

    public static string WebsiteAndOrganization(SeoUrlBuilder urls, SeoOptions options)
    {
        var graph = new object[]
        {
            new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["@id"] = urls.Absolute("/#website"),
                ["url"] = urls.PublicBaseUrl + "/",
                ["name"] = options.SiteName,
                ["inLanguage"] = "es-AR",
                ["publisher"] = new Dictionary<string, string> { ["@id"] = urls.Absolute("/#organization") }
            },
            new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["@id"] = urls.Absolute("/#organization"),
                ["name"] = options.OrganizationName,
                ["url"] = urls.PublicBaseUrl + "/",
                ["logo"] = urls.DefaultOgImage
            }
        };

        var doc = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph
        };
        return JsonSerializer.Serialize(doc, JsonOptions);
    }

    public static string? BreadcrumbList(IReadOnlyList<SeoBreadcrumbItem>? crumbs, SeoUrlBuilder urls)
    {
        if (crumbs == null || crumbs.Count == 0) return null;

        var elements = new List<object>();
        for (var i = 0; i < crumbs.Count; i++)
        {
            var c = crumbs[i];
            var item = new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["name"] = c.Name
            };
            if (!string.IsNullOrWhiteSpace(c.Path))
                item["item"] = urls.Absolute(c.Path);
            elements.Add(item);
        }

        var doc = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = elements
        };
        return JsonSerializer.Serialize(doc, JsonOptions);
    }
}

public sealed class SitemapUrlEntry
{
    public string Loc { get; init; } = "";
    public DateTime? LastMod { get; init; }
}

public static class SitemapXmlBuilder
{
    public static string BuildUrlSet(IEnumerable<SitemapUrlEntry> urls)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var u in urls)
        {
            if (string.IsNullOrWhiteSpace(u.Loc)) continue;
            sb.Append("<url>");
            sb.Append("<loc>").Append(XmlEscape(u.Loc)).Append("</loc>");
            if (u.LastMod.HasValue)
                sb.Append("<lastmod>").Append(u.LastMod.Value.ToUniversalTime().ToString("yyyy-MM-dd")).Append("</lastmod>");
            sb.Append("</url>");
        }
        sb.Append("</urlset>");
        return sb.ToString();
    }

    public static string BuildSitemapIndex(IEnumerable<(string Loc, DateTime? LastMod)> sitemaps)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.Append("<sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var (loc, lastMod) in sitemaps)
        {
            sb.Append("<sitemap>");
            sb.Append("<loc>").Append(XmlEscape(loc)).Append("</loc>");
            if (lastMod.HasValue)
                sb.Append("<lastmod>").Append(lastMod.Value.ToUniversalTime().ToString("yyyy-MM-dd")).Append("</lastmod>");
            sb.Append("</sitemap>");
        }
        sb.Append("</sitemapindex>");
        return sb.ToString();
    }

    private static string XmlEscape(string value) =>
        value.Replace("&", "&amp;", StringComparison.Ordinal)
             .Replace("<", "&lt;", StringComparison.Ordinal)
             .Replace(">", "&gt;", StringComparison.Ordinal)
             .Replace("\"", "&quot;", StringComparison.Ordinal)
             .Replace("'", "&apos;", StringComparison.Ordinal);
}
