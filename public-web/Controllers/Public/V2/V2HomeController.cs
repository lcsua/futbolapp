using Microsoft.AspNetCore.Mvc;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public.V2;

[Route("v2")]
public class V2HomeController : Controller
{
    private readonly LeaguePublicService _leagueService;

    public V2HomeController(LeaguePublicService leagueService)
    {
        _leagueService = leagueService;
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
        ViewData["Description"] = "Explorá torneos y ligas en la vista previa V2.";
        var leagues = await _leagueService.GetPublicLeaguesAsync();
        return View("~/Views/V2/Ligas.cshtml", leagues);
    }
}
