using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;

namespace PublicWeb.Controllers.Public;

[Route("partido")]
public class MatchController : Controller
{
    private readonly MatchPublicService _matchService;

    public MatchController(MatchPublicService matchService)
    {
        _matchService = matchService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Index(Guid id)
    {
        var model = await _matchService.GetMatchByIdAsync(id);
        if (model == null) return NotFound();

        return View("~/Views/Public/Match.cshtml", model);
    }
}

