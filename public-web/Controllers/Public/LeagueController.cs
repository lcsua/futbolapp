using PublicWeb.Models.Public;
using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public;

[Route("ligas")]
public class LeagueController : Controller
{
    private readonly LeaguePublicService _leagueService;
    private readonly TeamPublicService _teamService;
    private readonly ProfessionalFootballPublicService _professionalFootballService;

    public LeagueController(
        LeaguePublicService leagueService,
        TeamPublicService teamService,
        ProfessionalFootballPublicService professionalFootballService)
    {
        _leagueService = leagueService;
        _teamService = teamService;
        _professionalFootballService = professionalFootballService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var leagues = await _leagueService.GetPublicLeaguesAsync();
        var (tournaments, failed) = await _professionalFootballService.GetArgentineCompetitionsAsync();

        var model = new LeaguesIndexPageViewModel
        {
            AmateurLeagues = leagues,
            ArgentineTournaments = tournaments,
            ArgentineTournamentsUnavailable = failed,
        };

        return View("~/Views/Public/LeaguesIndex.cshtml", model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Details(string slug)
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

    [HttpGet("{slug}/{teamSlug}")]
    public async Task<IActionResult> Team(string slug, string teamSlug, [FromQuery] string? season)
    {
        // Reserved league sub-routes handled by more specific actions; guard just in case.
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var model = await _teamService.GetTeamSummaryAsync(slug, teamSlug, season);
        if (model == null) return NotFound();

        ViewBag.League = model.League ?? await _leagueService.GetLeagueBySlugAsync(slug);
        ViewBag.Seasons = await _leagueService.GetLeagueMetaAsync(slug);

        return View("~/Views/Public/Team.cshtml", model);
    }

    private static bool IsReservedTeamSlug(string teamSlug) =>
        teamSlug.Equals("tabla", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("resultados", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("partidos", StringComparison.OrdinalIgnoreCase);
}
