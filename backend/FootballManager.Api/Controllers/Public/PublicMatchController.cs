using System;
using System.Threading.Tasks;
using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[Route("api/public/matches")]
[AllowAnonymous]
public class PublicMatchController : ControllerBase
{
    private readonly PublicMatchService _matchService;

    public PublicMatchController(PublicMatchService matchService)
    {
        _matchService = matchService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetMatch(Guid id)
    {
        var match = await _matchService.GetMatchAsync(id, HttpContext.RequestAborted);
        if (match == null) return NotFound();
        return Ok(match);
    }
}
