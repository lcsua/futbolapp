using PublicWeb.Models.Public;
using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public;

/// <summary>
/// League index + legacy URL redirects. League pages are served by V2LeagueController on /ligas/{slug}/…
/// </summary>
[Route("ligas")]
public class LeagueController : Controller
{
    private readonly LeaguePublicService _leagueService;
    private readonly ProfessionalFootballPublicService _professionalFootballService;

    public LeagueController(
        LeaguePublicService leagueService,
        ProfessionalFootballPublicService professionalFootballService)
    {
        _leagueService = leagueService;
        _professionalFootballService = professionalFootballService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        ViewBag.V2ActiveNav = "ligas";
        ViewData["Title"] = "Ligas";
        ViewData["Description"] = "Explorá ligas amateur y torneos argentinos en Mi Liga.";

        var leagues = await _leagueService.GetPublicLeaguesAsync();
        var (tournaments, failed) = await _professionalFootballService.GetArgentineCompetitionsAsync();

        var model = new LeaguesIndexPageViewModel
        {
            AmateurLeagues = leagues,
            ArgentineTournaments = tournaments,
            ArgentineTournamentsUnavailable = failed,
        };

        return View("~/Views/V2/Ligas.cshtml", model);
    }

    /// <summary>Legacy: /tabla → /posiciones</summary>
    [HttpGet("{slug}/tabla")]
    public IActionResult StandingsRedirect(string slug)
    {
        return PermanentTo($"~/ligas/{slug}/posiciones");
    }

    /// <summary>Legacy: /partidos → /fixture</summary>
    [HttpGet("{slug}/partidos")]
    public IActionResult FixtureRedirect(string slug)
    {
        return PermanentTo($"~/ligas/{slug}/fixture");
    }

    [HttpGet("{slug}/documentos")]
    public async Task<IActionResult> DocumentsIndex(string slug)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();
        return RedirectPermanent(Url.Content($"~/ligas/{slug}/informacion")!);
    }

    [HttpGet("{slug}/documentos/{categorySlug}")]
    public async Task<IActionResult> Documents(string slug, string categorySlug)
    {
        var league = await _leagueService.GetLeagueBySlugAsync(slug);
        if (league == null) return NotFound();

        var docs = await _leagueService.GetDocumentsAsync(slug) ?? new LeagueDocumentsViewModel();
        var category = docs.Categories.FirstOrDefault(c =>
            c.Slug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));
        if (category == null)
            return RedirectPermanent(Url.Content($"~/ligas/{slug}/informacion")!);

        return RedirectPermanent(Url.Content($"~/ligas/{slug}/informacion#doc-{category.Slug}")!);
    }

    private IActionResult PermanentTo(string appRelativePath)
    {
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        return RedirectPermanent(Url.Content(appRelativePath)! + qs);
    }
}
