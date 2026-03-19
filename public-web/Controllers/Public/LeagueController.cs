using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public;

[Route("liga")]
public class LeagueController : Controller
{
    private readonly LeaguePublicService _leagueService;

    public LeagueController(LeaguePublicService leagueService)
    {
        _leagueService = leagueService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        var model = await _leagueService.GetLeagueBySlugAsync(slug);
        if (model == null) return NotFound();

        return View("~/Views/Public/League.cshtml", model);
    }

    [HttpGet("{slug}/tabla")]
    public async Task<IActionResult> Standings(string slug)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var standings = await _leagueService.GetStandingsAsync(slug);
        ViewBag.League = league;

        return View("~/Views/Public/Standings.cshtml", standings);
    }

    [HttpGet("{slug}/resultados")]
    public async Task<IActionResult> Results(string slug)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var results = await _leagueService.GetResultsAsync(slug);
        ViewBag.League = league;

        return View("~/Views/Public/Results.cshtml", results);
    }

    [HttpGet("{slug}/fixture")]
    public async Task<IActionResult> Fixture(string slug)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var fixture = await _leagueService.GetFixtureAsync(slug);
        ViewBag.League = league;

        return View("~/Views/Public/Fixture.cshtml", fixture);
    }
}
