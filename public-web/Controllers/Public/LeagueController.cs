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
    public async Task<IActionResult> Standings(string slug, [FromQuery] string? season, [FromQuery] string? division)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.Seasons = meta;
        ViewBag.League = league;
        ViewBag.Division = division ?? "all";

        var standings = await _leagueService.GetStandingsAsync(slug, season, division);
        
        return View("~/Views/Public/Standings.cshtml", standings);
    }

    [HttpGet("{slug}/resultados")]
    public async Task<IActionResult> Results(string slug, [FromQuery] string? season, [FromQuery] string? division, [FromQuery] int? round)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.Seasons = meta;
        ViewBag.League = league;
        ViewBag.Division = division ?? "all";
        ViewBag.Round = round;

        var results = await _leagueService.GetResultsAsync(slug, season, division, null);

        return View("~/Views/Public/Results.cshtml", results);
    }

    [HttpGet("{slug}/partidos")]
    public async Task<IActionResult> Fixture(string slug, [FromQuery] string? season, [FromQuery] string? division, [FromQuery] int? round)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.Seasons = meta;
        ViewBag.League = league;
        ViewBag.Division = division ?? "all";
        ViewBag.Round = round;

        var fixture = await _leagueService.GetFixtureAsync(slug, season, division, null);

        return View("~/Views/Public/Fixture.cshtml", fixture);
    }
}
