using Microsoft.AspNetCore.Mvc;
using PublicWeb.Helpers;
using PublicWeb.Models.Public;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public;

[Route("partido")]
public class MatchController : Controller
{
    private readonly MatchPublicService _matchService;
    private readonly LeaguePublicService _leagueService;

    public MatchController(MatchPublicService matchService, LeaguePublicService leagueService)
    {
        _matchService = matchService;
        _leagueService = leagueService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        MatchViewModel? model;
        if (Guid.TryParse(slug, out var id))
            model = await _matchService.GetMatchByIdAsync(id);
        else
            model = await _matchService.GetMatchBySlugAsync(slug);

        if (model == null) return NotFound();

        var canonicalSlug = MatchSlugHelper.FromMatch(model);
        if (!string.Equals(slug, canonicalSlug, StringComparison.OrdinalIgnoreCase))
        {
            if (Guid.TryParse(slug, out _))
            {
                var viaSlug = await _matchService.GetMatchBySlugAsync(canonicalSlug);
                if (viaSlug != null)
                    return RedirectPermanent(Url.Content($"~/partido/{canonicalSlug}")!);
            }
            else
            {
                return RedirectPermanent(Url.Content($"~/partido/{canonicalSlug}")!);
            }
        }

        LeagueViewModel? league = null;
        if (!string.IsNullOrWhiteSpace(model.LeagueSlug))
            league = await _leagueService.GetLeagueBySlugAsync(model.LeagueSlug);

        var leagueSlug = league?.Slug ?? model.LeagueSlug ?? string.Empty;
        var finished = TeamDisplayHelper.IsFinished(model.Status);

        ViewBag.League = league;
        ViewBag.LeagueSlug = leagueSlug;
        ViewBag.SeasonName = await ResolveSeasonNameAsync(model);
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2LeagueTab = finished ? "resultados" : "fixture";
        ViewBag.BackHref = ResolveBackHref(leagueSlug, finished);
        ViewBag.BackLabel = ResolveBackLabel(league);

        return View("~/Views/V2/Match.cshtml", model);
    }

    private string ResolveBackHref(string leagueSlug, bool finished)
    {
        var referer = Request.Headers.Referer.ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri)
            && string.Equals(uri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase)
            && !uri.AbsolutePath.Contains("/partido/", StringComparison.OrdinalIgnoreCase))
        {
            return uri.PathAndQuery;
        }

        if (!string.IsNullOrWhiteSpace(leagueSlug))
        {
            var tab = finished ? "resultados" : "fixture";
            return Url.Content($"~/ligas/{leagueSlug}/{tab}")!;
        }

        return Url.Content("~/ligas")!;
    }

    private static string ResolveBackLabel(LeagueViewModel? league)
    {
        if (!string.IsNullOrWhiteSpace(league?.Name))
            return league.Name;
        return "Volver";
    }

    private async Task<string?> ResolveSeasonNameAsync(MatchViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.LeagueSlug) || string.IsNullOrWhiteSpace(model.SeasonSlug))
            return null;

        var meta = await _leagueService.GetLeagueMetaAsync(model.LeagueSlug);
        return meta.FirstOrDefault(s =>
            string.Equals(s.Slug, model.SeasonSlug, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}
