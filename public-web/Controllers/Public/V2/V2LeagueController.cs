using Microsoft.AspNetCore.Mvc;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public.V2;

/// <summary>
/// Public UI V2 — isolated routes under /v2/ligas. Reuses V1 services/models; does not alter V1 routes.
/// </summary>
[Route("v2/ligas")]
public class V2LeagueController : Controller
{
    private readonly LeaguePublicService _leagueService;
    private readonly TeamPublicService _teamService;

    public V2LeagueController(LeaguePublicService leagueService, TeamPublicService teamService)
    {
        _leagueService = leagueService;
        _teamService = teamService;
    }

    [HttpGet("{slug}/{teamSlug}")]
    public async Task<IActionResult> Team(
        string slug,
        string teamSlug,
        [FromQuery] string? season,
        [FromQuery] int nextPage = 1,
        [FromQuery] int resultsPage = 1)
    {
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var model = await _teamService.GetTeamSummaryAsync(slug, teamSlug, season, nextPage, resultsPage);
        if (model == null) return NotFound();

        ViewBag.League = model.League ?? await _leagueService.GetLeagueBySlugAsync(slug);
        ViewBag.Seasons = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2TeamTab = "resumen";

        return View("~/Views/V2/Team.cshtml", model);
    }

    [HttpGet("{slug}/{teamSlug}/pagina")]
    public async Task<IActionResult> TeamMatchesPage(
        string slug,
        string teamSlug,
        [FromQuery] string? season,
        [FromQuery] int nextPage = 1,
        [FromQuery] int resultsPage = 1)
    {
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var model = await _teamService.GetTeamSummaryAsync(slug, teamSlug, season, nextPage, resultsPage);
        if (model == null) return NotFound();

        return Json(new
        {
            nextMatches = model.NextMatches.Select(m => new
            {
                id = m.Id,
                kickoff = m.Kickoff,
                status = m.Status,
                homeScore = m.HomeScore,
                awayScore = m.AwayScore,
                homeTeam = m.HomeTeam.Name,
                awayTeam = m.AwayTeam.Name,
                fieldName = m.FieldName
            }),
            lastResults = model.LastResults.Select(m => new
            {
                id = m.Id,
                kickoff = m.Kickoff,
                status = m.Status,
                homeScore = m.HomeScore,
                awayScore = m.AwayScore,
                homeTeam = m.HomeTeam.Name,
                awayTeam = m.AwayTeam.Name,
                fieldName = m.FieldName
            }),
            nextMatchesPage = model.NextMatchesPage,
            nextMatchesTotal = model.NextMatchesTotal,
            nextMatchesTotalPages = model.NextMatchesTotalPages,
            lastResultsPage = model.LastResultsPage,
            lastResultsTotal = model.LastResultsTotal,
            lastResultsTotalPages = model.LastResultsTotalPages
        });
    }

    private static bool IsReservedTeamSlug(string teamSlug) =>
        teamSlug.Equals("tabla", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("resultados", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("partidos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("documentos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("posiciones", StringComparison.OrdinalIgnoreCase);
}
