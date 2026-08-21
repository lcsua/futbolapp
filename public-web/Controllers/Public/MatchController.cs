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

    [HttpGet("{id}")]
    public async Task<IActionResult> Index(Guid id)
    {
        var model = await _matchService.GetMatchByIdAsync(id);
        if (model == null) return NotFound();

        LeagueViewModel? league = null;
        if (!string.IsNullOrWhiteSpace(model.LeagueSlug))
            league = await _leagueService.GetLeagueBySlugAsync(model.LeagueSlug);

        var leagueSlug = league?.Slug ?? model.LeagueSlug ?? string.Empty;
        var finished = TeamDisplayHelper.IsFinished(model.Status);

        ViewBag.League = league;
        ViewBag.LeagueSlug = leagueSlug;
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
}
