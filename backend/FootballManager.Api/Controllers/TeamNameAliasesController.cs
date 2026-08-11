using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Teams.GetTeamNameAliases;
using FootballManager.Application.UseCases.Teams.UpsertTeamNameAliases;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballManager.Api.Controllers;

[ApiController]
[Route("api/leagues/{leagueId}/team-name-aliases")]
public class TeamNameAliasesController : ControllerBase
{
    private readonly IGetTeamNameAliasesUseCase _getUseCase;
    private readonly IUpsertTeamNameAliasesUseCase _upsertUseCase;

    public TeamNameAliasesController(
        IGetTeamNameAliasesUseCase getUseCase,
        IUpsertTeamNameAliasesUseCase upsertUseCase)
    {
        _getUseCase = getUseCase ?? throw new ArgumentNullException(nameof(getUseCase));
        _upsertUseCase = upsertUseCase ?? throw new ArgumentNullException(nameof(upsertUseCase));
    }

    [HttpGet]
    public async Task<IActionResult> GetByLeague(Guid leagueId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var response = await _getUseCase.ExecuteAsync(new GetTeamNameAliasesRequest
        {
            LeagueId = leagueId,
            UserId = userId,
        }, cancellationToken);

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(
        Guid leagueId,
        [FromBody] UpsertTeamNameAliasesBody body,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var response = await _upsertUseCase.ExecuteAsync(new UpsertTeamNameAliasesRequest
        {
            LeagueId = leagueId,
            UserId = userId,
            Items = body.Items ?? new(),
        }, cancellationToken);

        return Ok(response);
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Guid.Empty;
        return userId;
    }
}

public class UpsertTeamNameAliasesBody
{
    public System.Collections.Generic.List<TeamNameAliasItemDto>? Items { get; set; }
}
