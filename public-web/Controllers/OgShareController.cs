using Microsoft.AspNetCore.Mvc;
using PublicWeb.Seo;

namespace PublicWeb.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class OgShareController : Controller
{
    private readonly OgShareImageGenerator _images;

    public OgShareController(OgShareImageGenerator images)
    {
        _images = images;
    }

    [AcceptVerbs("GET", "HEAD")]
    [Route("/og/share.jpg")]
    [Route("/og/share.png")]
    public IActionResult Share(
        [FromQuery] string? kind,
        [FromQuery] string? division,
        [FromQuery] string? league,
        [FromQuery] string? season)
    {
        var jpeg = _images.Render(
            Normalize(kind, 24),
            Normalize(division, 40),
            Normalize(league, 80),
            string.IsNullOrWhiteSpace(season) ? null : Normalize(season, 48));
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(jpeg, "image/jpeg");
    }

    private static string Normalize(string? value, int max)
    {
        var t = (value ?? "").Trim();
        if (t.Length == 0) return "";
        return t.Length <= max ? t : t[..max];
    }
}
