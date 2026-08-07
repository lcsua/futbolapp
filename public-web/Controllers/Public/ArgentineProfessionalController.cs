using Microsoft.AspNetCore.Mvc;
using PublicWeb.Services.Public;

namespace PublicWeb.Controllers.Public;

[Route("ligas/argentina")]
public class ArgentineProfessionalController : Controller
{
    private readonly ProfessionalFootballPublicService _service;

    public ArgentineProfessionalController(ProfessionalFootballPublicService service)
    {
        _service = service;
    }

    [HttpGet("{competitionSlug}")]
    public async Task<IActionResult> Details(string competitionSlug)
    {
        var model = await _service.GetCompetitionDetailAsync(competitionSlug);
        if (model == null)
            return NotFound();

        ViewData["Title"] = model.Competition.Name;
        ViewData["Description"] = $"{model.Competition.Name} — {model.Competition.CurrentTournament} {model.Competition.Season}";
        return View("~/Views/Public/ProfessionalCompetition.cshtml", model);
    }
}
