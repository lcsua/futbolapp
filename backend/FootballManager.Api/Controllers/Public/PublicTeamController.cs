using System.Threading.Tasks;
using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[Route("api/public/teams")]
[AllowAnonymous]
public class PublicTeamController : ControllerBase
{
    private readonly PublicTeamService _teamService;

    public PublicTeamController(PublicTeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetTeam(string slug)
    {
        var team = await _teamService.GetTeamAsync(slug, HttpContext.RequestAborted);
        if (team == null) return NotFound();
        return Ok(team);
    }
}
