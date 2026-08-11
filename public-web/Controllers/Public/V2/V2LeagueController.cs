using Microsoft.AspNetCore.Mvc;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public.V2;

/// <summary>
/// Public UI V2 — isolated routes under /v2/ligas. Reuses V1 services/models; does not alter V1 routes.
/// </summary>
[Route("v2/ligas")]
public class V2LeagueController : Controller
{
    private const int SummaryPageSize = 5;
    private const int PartidosPageSize = 10;

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
        [FromQuery] string? tab,
        [FromQuery] int? resultadosPage,
        [FromQuery] int? proximosPage,
        // Compat with existing / original query names
        [FromQuery] int? resultsPage,
        [FromQuery] int? nextPage)
    {
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var activeTab = NormalizeTab(tab);
        var rPage = Math.Max(1, resultadosPage ?? resultsPage ?? 1);
        var nPage = Math.Max(1, proximosPage ?? nextPage ?? 1);

        // Resumen / estadísticas: siempre página 1 (featured + forma). Partidos: paginación real.
        var pageSize = activeTab == "partidos" ? PartidosPageSize : SummaryPageSize;
        var requestResultsPage = activeTab == "partidos" ? rPage : 1;
        var requestNextPage = activeTab == "partidos" ? nPage : 1;

        var model = await _teamService.GetTeamSummaryAsync(
            slug, teamSlug, season, requestNextPage, requestResultsPage, pageSize);
        if (model == null) return NotFound();

        ViewBag.League = model.League ?? await _leagueService.GetLeagueBySlugAsync(slug);
        ViewBag.Seasons = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2TeamTab = activeTab;
        ViewBag.V2ResultadosPage = model.LastResultsPage;
        ViewBag.V2ProximosPage = model.NextMatchesPage;

        return View("~/Views/V2/Team.cshtml", model);
    }

    [HttpGet("{slug}/{teamSlug}/pagina")]
    public async Task<IActionResult> TeamMatchesPage(
        string slug,
        string teamSlug,
        [FromQuery] string? season,
        [FromQuery] int? resultadosPage,
        [FromQuery] int? proximosPage,
        [FromQuery] int? resultsPage,
        [FromQuery] int? nextPage)
    {
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var rPage = Math.Max(1, resultadosPage ?? resultsPage ?? 1);
        var nPage = Math.Max(1, proximosPage ?? nextPage ?? 1);

        var model = await _teamService.GetTeamSummaryAsync(
            slug, teamSlug, season, nPage, rPage, PartidosPageSize);
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

    private static string NormalizeTab(string? tab)
    {
        var t = (tab ?? "resumen").Trim().ToLowerInvariant();
        return t is "partidos" or "estadisticas" ? t : "resumen";
    }

    private static bool IsReservedTeamSlug(string teamSlug) =>
        teamSlug.Equals("tabla", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("resultados", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("partidos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("documentos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("posiciones", StringComparison.OrdinalIgnoreCase);
}
