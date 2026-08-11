using Microsoft.AspNetCore.Mvc;
using PublicWeb.Models.Public;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public.V2;

[Route("v2")]
public class V2HomeController : Controller
{
    private readonly LeaguePublicService _leagueService;
    private readonly ProfessionalFootballPublicService _professionalFootballService;

    public V2HomeController(
        LeaguePublicService leagueService,
        ProfessionalFootballPublicService professionalFootballService)
    {
        _leagueService = leagueService;
        _professionalFootballService = professionalFootballService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        ViewBag.V2ActiveNav = "home";
        ViewData["Title"] = "Vista previa V2";
        return View("~/Views/V2/Home.cshtml");
    }

    [HttpGet("ligas")]
    public async Task<IActionResult> Ligas()
    {
        ViewBag.V2ActiveNav = "ligas";
        ViewData["Title"] = "Ligas";
        ViewData["Description"] = "Explorá ligas y torneos argentinos en la vista previa V2.";

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
}
