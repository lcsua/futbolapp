using System.Threading.Tasks;
using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

/// <summary>
/// Public API with SEO-friendly URL structure: /liga/{leagueSlug}/torneo/{season}/...
/// Only returns data when League.IsPublic == true.
/// </summary>
[ApiController]
[Route("api/public/liga")]
[AllowAnonymous]
public class PublicStructuredController : ControllerBase
{
    private readonly PublicLeagueService _leagueService;
    private readonly PublicStructuredService _structuredService;

    public PublicStructuredController(PublicLeagueService leagueService, PublicStructuredService structuredService)
    {
        _leagueService = leagueService;
        _structuredService = structuredService;
    }

    /// <summary>
    /// Team summary: active seasons, next matches, last results, basic stats.
    /// </summary>
    [HttpGet("{leagueSlug}/torneo/{season}/equipo/{teamSlug}")]
    public async Task<IActionResult> GetTeamSummary(string leagueSlug, string season, string teamSlug)
    {
        var result = await _structuredService.GetTeamSummaryAsync(leagueSlug, season, teamSlug, HttpContext.RequestAborted);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Division summary: standings table, teams.
    /// </summary>
    [HttpGet("{leagueSlug}/torneo/{season}/division/{divisionSlug}")]
    public async Task<IActionResult> GetDivisionSummary(string leagueSlug, string season, string divisionSlug)
    {
        var result = await _structuredService.GetDivisionSummaryAsync(leagueSlug, season, divisionSlug, HttpContext.RequestAborted);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Division results (completed matches).
    /// </summary>
    [HttpGet("{leagueSlug}/torneo/{season}/division/{divisionSlug}/resultados")]
    public async Task<IActionResult> GetDivisionResults(string leagueSlug, string season, string divisionSlug)
    {
        var result = await _structuredService.GetDivisionResultsAsync(leagueSlug, season, divisionSlug, HttpContext.RequestAborted);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Division matches (upcoming).
    /// </summary>
    [HttpGet("{leagueSlug}/torneo/{season}/division/{divisionSlug}/partidos")]
    public async Task<IActionResult> GetDivisionMatches(string leagueSlug, string season, string divisionSlug)
    {
        var result = await _structuredService.GetDivisionMatchesAsync(leagueSlug, season, divisionSlug, HttpContext.RequestAborted);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
