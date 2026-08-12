using Microsoft.AspNetCore.Mvc;
using PublicWeb.Seo;

namespace PublicWeb.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class SeoController : Controller
{
    private readonly SitemapDocumentService _sitemap;

    public SeoController(SitemapDocumentService sitemap)
    {
        _sitemap = sitemap;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var xml = await _sitemap.GetSitemapXmlAsync(cancellationToken);
        return Content(xml, "application/xml; charset=utf-8");
    }

    /// <summary>
    /// Reserved for future sitemap index children (pages/leagues/teams).
    /// Currently returns the same urlset as /sitemap.xml.
    /// </summary>
    [HttpGet("/sitemaps/{name}.xml")]
    [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> SitemapSection(string name, CancellationToken cancellationToken)
    {
        if (!string.Equals(name, "all", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(name, "pages", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(name, "leagues", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(name, "teams", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var xml = await _sitemap.GetSitemapXmlAsync(cancellationToken);
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult Robots()
    {
        return Content(_sitemap.GetRobotsTxt(), "text/plain; charset=utf-8");
    }
}
