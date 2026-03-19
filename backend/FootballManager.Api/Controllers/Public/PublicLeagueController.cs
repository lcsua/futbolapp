using System.Threading.Tasks;
using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[Route("api/public/leagues")]
[AllowAnonymous]
public class PublicLeagueController : ControllerBase
{
    private readonly PublicLeagueService _leagueService;

    public PublicLeagueController(PublicLeagueService leagueService)
    {
        _leagueService = leagueService;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetLeague(string slug)
    {
        var model = await _leagueService.GetLeagueAsync(slug, HttpContext.RequestAborted);
        if (model == null) return NotFound();
        return Ok(model);
    }

    [HttpGet("{slug}/standings")]
    public async Task<IActionResult> GetStandings(string slug)
    {
        var standings = await _leagueService.GetStandingsAsync(slug, HttpContext.RequestAborted);
        return Ok(standings);
    }

    [HttpGet("{slug}/results")]
    public async Task<IActionResult> GetResults(string slug)
    {
        var results = await _leagueService.GetResultsAsync(slug, HttpContext.RequestAborted);
        return Ok(results);
    }

    [HttpGet("{slug}/fixture")]
    public async Task<IActionResult> GetFixture(string slug)
    {
        var fixture = await _leagueService.GetFixtureAsync(slug, HttpContext.RequestAborted);
        return Ok(fixture);
    }
}
