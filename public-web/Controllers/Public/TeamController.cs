using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public;

[Route("equipo")]
public class TeamController : Controller
{
    private readonly TeamPublicService _teamService;

    public TeamController(TeamPublicService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        var model = await _teamService.GetTeamBySlugAsync(slug);
        if (model == null) return NotFound();

        return View("~/Views/Public/Team.cshtml", model);
    }
}
