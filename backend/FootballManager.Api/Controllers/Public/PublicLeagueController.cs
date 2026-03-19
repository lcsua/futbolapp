using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[Route("api/public/liga/{leagueSlug}")]
[AllowAnonymous]
public class PublicLeagueController : ControllerBase
{
    private readonly PublicStructuredService _service;

    public PublicLeagueController(PublicStructuredService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeague(string leagueSlug)
    {
        var result = await _service.GetLeagueSummaryAsync(leagueSlug);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("torneo/{seasonSlug}/equipo/{teamSlug}")]
    public async Task<IActionResult> GetTeamSummary(string leagueSlug, string seasonSlug, string teamSlug)
    {
        var result = await _service.GetTeamSummaryAsync(leagueSlug, seasonSlug, teamSlug);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("meta")]
    public async Task<IActionResult> GetLeagueMeta(string leagueSlug)
    {
        var result = await _service.GetLeagueMetaAsync(leagueSlug);
        return Ok(result);
    }

    [HttpGet("tabla")]
    public async Task<IActionResult> GetStandings(string leagueSlug, [FromQuery] string? season)
    {
        var result = await _service.GetLeagueStandingsAsync(leagueSlug, season);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("resultados")]
    public async Task<IActionResult> GetResults(string leagueSlug, [FromQuery] string? season)
    {
        var result = await _service.GetLeagueResultsAsync(leagueSlug, season);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("partidos")]
    public async Task<IActionResult> GetMatches(string leagueSlug, [FromQuery] string? season)
    {
        var result = await _service.GetLeagueMatchesAsync(leagueSlug, season);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
