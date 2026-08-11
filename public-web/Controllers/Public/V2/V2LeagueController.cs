using Microsoft.AspNetCore.Mvc;
using PublicWeb.Helpers;
using PublicWeb.Models.Public;
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
    private const int HomeMatchPreviewCount = 5;
    private const int HomeStandingsTop = 4;
    private const int HomeStandingsMaxDivisions = 4;
    private const int HomeSampleTeams = 8;

    private readonly LeaguePublicService _leagueService;
    private readonly TeamPublicService _teamService;

    public V2LeagueController(LeaguePublicService leagueService, TeamPublicService teamService)
    {
        _leagueService = leagueService;
        _teamService = teamService;
    }

    /// <summary>League home / Resumen — must be registered before Team ({slug}/{teamSlug}).</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Home(string slug, [FromQuery] string? season)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        var selectedSeason = meta.FirstOrDefault(s =>
                                !string.IsNullOrWhiteSpace(season)
                                && string.Equals(s.Slug, season, StringComparison.OrdinalIgnoreCase))
            ?? meta.FirstOrDefault(s => s.IsActive)
            ?? meta.FirstOrDefault();
        var seasonSlug = selectedSeason?.Slug;

        var resultsTask = _leagueService.GetResultsAsync(slug, seasonSlug, null, null);
        var fixtureTask = _leagueService.GetFixtureAsync(slug, seasonSlug, null, null);
        var standingsTask = _leagueService.GetStandingsAsync(slug, seasonSlug, null);
        await Task.WhenAll(resultsTask, fixtureTask, standingsTask);

        var results = await resultsTask;
        var fixture = await fixtureTask;
        var standings = await standingsTask;

        var recent = FlattenMatches(results)
            .OrderByDescending(m => m.Kickoff)
            .Take(HomeMatchPreviewCount)
            .ToList();

        var upcoming = FlattenMatches(fixture)
            .OrderBy(m => m.Kickoff)
            .Take(HomeMatchPreviewCount)
            .ToList();

        var previews = (standings?.Divisions ?? new List<DivisionGroupViewModel<StandingsRowViewModel>>())
            .Where(d => d.Data != null && d.Data.Any())
            .Take(HomeStandingsMaxDivisions)
            .Select(d => new StandingsPreviewGroupViewModel
            {
                DivisionName = d.DivisionName,
                DivisionSlug = d.DivisionSlug,
                Rows = d.Data.OrderBy(r => r.Position).Take(HomeStandingsTop).ToList()
            })
            .ToList();

        var sampleTeams = (standings?.Divisions ?? new List<DivisionGroupViewModel<StandingsRowViewModel>>())
            .SelectMany(d => d.Data ?? new List<StandingsRowViewModel>())
            .OrderBy(r => r.Position)
            .Select(r => r.Team)
            .Where(t => !string.IsNullOrWhiteSpace(t.Slug))
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .Take(HomeSampleTeams)
            .ToList();

        var model = new LeagueHomeViewModel
        {
            League = league,
            SeasonName = results?.SeasonName ?? fixture?.SeasonName ?? standings?.SeasonName ?? selectedSeason?.Name ?? string.Empty,
            SeasonSlug = results?.SeasonSlug ?? fixture?.SeasonSlug ?? standings?.SeasonSlug ?? selectedSeason?.Slug ?? string.Empty,
            Divisions = selectedSeason?.Divisions ?? new List<DivisionViewModel>(),
            RecentResults = recent,
            UpcomingMatches = upcoming,
            StandingsPreviews = previews,
            SampleTeams = sampleTeams
        };

        ViewBag.League = league;
        ViewBag.Seasons = meta;
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2LeagueTab = "resumen";
        ViewBag.SeasonName = model.SeasonName;
        ViewBag.SeasonSlug = model.SeasonSlug;
        ViewBag.LeagueSlug = league.Slug;
        ViewBag.PageLabel = "Resumen";

        return View("~/Views/V2/LeagueHome.cshtml", model);
    }

    /// <summary>League results — must be registered before Team ({slug}/{teamSlug}).</summary>
    [HttpGet("{slug}/resultados")]
    public async Task<IActionResult> Results(
        string slug,
        [FromQuery] string? season,
        [FromQuery] string? division,
        [FromQuery] int? round)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        // Same as V1: load all rounds for pills; round filter applied in the view.
        var results = await _leagueService.GetResultsAsync(slug, season, division, null);

        ViewBag.League = league;
        ViewBag.Seasons = meta;
        ViewBag.Division = string.IsNullOrWhiteSpace(division) ? "all" : division;
        ViewBag.Round = round;
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2LeagueTab = "resultados";

        return View("~/Views/V2/Results.cshtml", results);
    }

    /// <summary>League standings — must be registered before Team ({slug}/{teamSlug}).</summary>
    [HttpGet("{slug}/posiciones")]
    public async Task<IActionResult> Standings(
        string slug,
        [FromQuery] string? season,
        [FromQuery] string? division)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        var standings = await _leagueService.GetStandingsAsync(slug, season, division);

        ViewBag.League = league;
        ViewBag.Seasons = meta;
        ViewBag.Division = string.IsNullOrWhiteSpace(division) ? "all" : division;
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2LeagueTab = "posiciones";

        return View("~/Views/V2/Standings.cshtml", standings);
    }

    /// <summary>League fixture — must be registered before Team ({slug}/{teamSlug}).</summary>
    [HttpGet("{slug}/fixture")]
    public async Task<IActionResult> Fixture(
        string slug,
        [FromQuery] string? season,
        [FromQuery] string? division,
        [FromQuery] int? fecha,
        [FromQuery] int? round)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var meta = await _leagueService.GetLeagueMetaAsync(slug);
        var calendar = await LoadFixtureCalendarAsync(slug, season, division);

        ViewBag.League = league;
        ViewBag.Seasons = meta;
        ViewBag.Division = string.IsNullOrWhiteSpace(division) ? "all" : division;
        ViewBag.Fecha = fecha ?? round;
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2LeagueTab = "fixture";
        ViewBag.SeasonName = calendar?.SeasonName;
        ViewBag.SeasonSlug = calendar?.SeasonSlug;
        ViewBag.LeagueSlug = league.Slug;

        return View("~/Views/V2/Fixture.cshtml", calendar);
    }

    /// <summary>
    /// HTML fragment for in-page fecha navigation.
    /// With a concrete division: one independent block. With all: full board.
    /// </summary>
    [HttpGet("{slug}/fixture/fragment")]
    public async Task<IActionResult> FixtureFragment(
        string slug,
        [FromQuery] string? season,
        [FromQuery] string? division,
        [FromQuery] int? fecha,
        [FromQuery] int? round)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var calendar = await LoadFixtureCalendarAsync(slug, season, division);
        if (calendar == null) return NotFound();

        var selectedDivision = string.IsNullOrWhiteSpace(division) ? "all" : division;
        var selectedFecha = fecha ?? round;

        ViewBag.League = league;
        ViewBag.Division = selectedDivision;
        ViewBag.Fecha = selectedFecha;
        ViewBag.LeagueSlug = league.Slug;
        ViewBag.SeasonSlug = calendar.SeasonSlug;

        // Per-division AJAX navigation: return only that block.
        if (!string.Equals(selectedDivision, "all", StringComparison.OrdinalIgnoreCase)
            && calendar.Divisions.Count == 1)
        {
            ViewData["LeagueSlug"] = league.Slug;
            ViewData["SeasonSlug"] = calendar.SeasonSlug;
            ViewData["PageDivision"] = selectedDivision;
            ViewData["BlockFecha"] = selectedFecha;
            return PartialView("~/Views/V2/Fixture/_FixtureDivisionBlock.cshtml", calendar.Divisions[0]);
        }

        return PartialView("~/Views/V2/Fixture/_FixtureBoard.cshtml", calendar);
    }

    private async Task<SeasonGroupedViewModel<MatchdayGroupViewModel>?> LoadFixtureCalendarAsync(
        string slug,
        string? season,
        string? division)
    {
        var resultsTask = _leagueService.GetResultsAsync(slug, season, division, null);
        var upcomingTask = _leagueService.GetFixtureAsync(slug, season, division, null);
        await Task.WhenAll(resultsTask, upcomingTask);

        var results = await resultsTask;
        var upcoming = await upcomingTask;
        if (results == null && upcoming == null) return null;

        return FixtureCalendarHelper.MergeCalendar(results, upcoming);
    }

    [HttpGet("{slug}/{teamSlug}")]
    public async Task<IActionResult> Team(
        string slug,
        string teamSlug,
        [FromQuery] string? season,
        [FromQuery] string? tab,
        [FromQuery] int? resultadosPage,
        [FromQuery] int? proximosPage,
        [FromQuery] int? resultsPage,
        [FromQuery] int? nextPage)
    {
        if (IsReservedTeamSlug(teamSlug))
            return NotFound();

        var activeTab = NormalizeTab(tab);
        var rPage = Math.Max(1, resultadosPage ?? resultsPage ?? 1);
        var nPage = Math.Max(1, proximosPage ?? nextPage ?? 1);

        TeamDetailViewModel partidos;
        TeamDetailViewModel summary;

        if (rPage == 1 && nPage == 1)
        {
            var firstPage = await _teamService.GetTeamSummaryAsync(
                slug, teamSlug, season, nextPage: 1, resultsPage: 1, pageSize: PartidosPageSize);
            if (firstPage == null) return NotFound();
            partidos = firstPage;
            summary = firstPage;
        }
        else
        {
            var recent = await _teamService.GetTeamSummaryAsync(
                slug, teamSlug, season, nextPage: 1, resultsPage: 1, pageSize: SummaryPageSize);
            if (recent == null) return NotFound();
            summary = recent;

            partidos = await _teamService.GetTeamSummaryAsync(
                slug, teamSlug, season, nPage, rPage, PartidosPageSize) ?? recent;
        }

        ViewBag.League = partidos.League ?? summary.League ?? await _leagueService.GetLeagueBySlugAsync(slug);
        ViewBag.Seasons = await _leagueService.GetLeagueMetaAsync(slug);
        ViewBag.V2ActiveNav = "ligas";
        ViewBag.V2TeamTab = activeTab;
        ViewBag.V2Summary = summary;

        return View("~/Views/V2/Team.cshtml", partidos);
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

    private static IEnumerable<MatchViewModel> FlattenMatches(SeasonGroupedViewModel<MatchdayGroupViewModel>? grouped)
    {
        if (grouped?.Divisions == null) yield break;
        foreach (var div in grouped.Divisions)
        {
            if (div.Data == null) continue;
            foreach (var day in div.Data)
            {
                if (day.Matches == null) continue;
                foreach (var match in day.Matches)
                    yield return match;
            }
        }
    }

    private static bool IsReservedTeamSlug(string teamSlug) =>
        teamSlug.Equals("tabla", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("resultados", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("partidos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("documentos", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("posiciones", StringComparison.OrdinalIgnoreCase) ||
        teamSlug.Equals("fixture", StringComparison.OrdinalIgnoreCase);
}
