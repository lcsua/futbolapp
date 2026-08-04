using Microsoft.AspNetCore.Mvc;
using PublicWeb.Helpers;

namespace PublicWeb.Controllers.Public;

/// <summary>
/// Permanent redirects from legacy /liga URLs to /ligas, cleaning redundant liga/ligas tokens in the slug.
/// </summary>
[Route("liga")]
public class LegacyLeagueRedirectController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectPermanent(Url.Content("~/ligas")!);
    }

    [HttpGet("{*path}")]
    public IActionResult CatchAll(string path)
    {
        var targetPath = RewriteLegacyPath(path);
        return RedirectPermanent(Url.Content($"~/ligas/{targetPath}")!);
    }

    internal static string RewriteLegacyPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var trimmed = path.Trim('/');
        var slashIndex = trimmed.IndexOf('/');
        var slugPart = slashIndex >= 0 ? trimmed[..slashIndex] : trimmed;
        var rest = slashIndex >= 0 ? trimmed[(slashIndex + 1)..] : string.Empty;

        var cleanedSlug = LeagueSlugHelper.CleanLeagueSlug(slugPart);
        if (string.IsNullOrEmpty(cleanedSlug))
            cleanedSlug = slugPart;

        return string.IsNullOrEmpty(rest) ? cleanedSlug : $"{cleanedSlug}/{rest}";
    }
}
