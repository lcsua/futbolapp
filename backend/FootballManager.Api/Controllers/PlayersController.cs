using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Players.CreatePlayer;
using FootballManager.Application.UseCases.Players.DeletePlayer;
using FootballManager.Application.UseCases.Players.GetPlayersByTeamIds;
using FootballManager.Application.UseCases.Players.GetTeamPlayers;
using FootballManager.Application.UseCases.Players.ImportPlayers;
using FootballManager.Application.UseCases.Players.UpdatePlayer;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers;

[ApiController]
[Route("api/leagues/{leagueId}/teams")]
public class PlayersController : ControllerBase
{
    private readonly IGetTeamPlayersUseCase _getTeamPlayersUseCase;
    private readonly IGetPlayersByTeamIdsUseCase _getPlayersByTeamIdsUseCase;
    private readonly ICreatePlayerUseCase _createPlayerUseCase;
    private readonly IUpdatePlayerUseCase _updatePlayerUseCase;
    private readonly IDeletePlayerUseCase _deletePlayerUseCase;
    private readonly IImportPlayersUseCase _importPlayersUseCase;

    public PlayersController(
        IGetTeamPlayersUseCase getTeamPlayersUseCase,
        IGetPlayersByTeamIdsUseCase getPlayersByTeamIdsUseCase,
        ICreatePlayerUseCase createPlayerUseCase,
        IUpdatePlayerUseCase updatePlayerUseCase,
        IDeletePlayerUseCase deletePlayerUseCase,
        IImportPlayersUseCase importPlayersUseCase)
    {
        _getTeamPlayersUseCase = getTeamPlayersUseCase ?? throw new ArgumentNullException(nameof(getTeamPlayersUseCase));
        _getPlayersByTeamIdsUseCase = getPlayersByTeamIdsUseCase ?? throw new ArgumentNullException(nameof(getPlayersByTeamIdsUseCase));
        _createPlayerUseCase = createPlayerUseCase ?? throw new ArgumentNullException(nameof(createPlayerUseCase));
        _updatePlayerUseCase = updatePlayerUseCase ?? throw new ArgumentNullException(nameof(updatePlayerUseCase));
        _deletePlayerUseCase = deletePlayerUseCase ?? throw new ArgumentNullException(nameof(deletePlayerUseCase));
        _importPlayersUseCase = importPlayersUseCase ?? throw new ArgumentNullException(nameof(importPlayersUseCase));
    }

    [HttpGet("players")]
    public async Task<IActionResult> GetPlayersByTeamIds(
        [FromRoute] Guid leagueId,
        [FromQuery] string? teamIds,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var ids = ParseTeamIds(teamIds);
        var response = await _getPlayersByTeamIdsUseCase.ExecuteAsync(
            new GetPlayersByTeamIdsRequest
            {
                LeagueId = leagueId,
                UserId = userId,
                TeamIds = ids
            },
            cancellationToken);

        return Ok(response.Players);
    }

    [HttpGet("{teamId:guid}/players")]
    public async Task<IActionResult> GetTeamPlayers(
        [FromRoute] Guid leagueId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var response = await _getTeamPlayersUseCase.ExecuteAsync(
            new GetTeamPlayersRequest
            {
                LeagueId = leagueId,
                TeamId = teamId,
                UserId = userId
            },
            cancellationToken);

        return Ok(response.Players);
    }

    [HttpPost("{teamId:guid}/players")]
    public async Task<IActionResult> CreatePlayer(
        [FromRoute] Guid leagueId,
        [FromRoute] Guid teamId,
        [FromBody] PlayerWriteBody body,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var response = await _createPlayerUseCase.ExecuteAsync(
            new CreatePlayerRequest
            {
                LeagueId = leagueId,
                TeamId = teamId,
                UserId = userId,
                FirstName = body?.FirstName ?? string.Empty,
                LastName = body?.LastName ?? string.Empty,
                Nickname = body?.Nickname,
                Document = body?.Document,
                Position = body?.Position,
                BirthDate = body?.BirthDate
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetTeamPlayers),
            new { leagueId, teamId },
            new { id = response.Id });
    }

    [HttpPut("{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> UpdatePlayer(
        [FromRoute] Guid leagueId,
        [FromRoute] Guid teamId,
        [FromRoute] Guid playerId,
        [FromBody] PlayerWriteBody body,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _updatePlayerUseCase.ExecuteAsync(
            new UpdatePlayerRequest
            {
                LeagueId = leagueId,
                TeamId = teamId,
                PlayerId = playerId,
                UserId = userId,
                FirstName = body?.FirstName ?? string.Empty,
                LastName = body?.LastName ?? string.Empty,
                Nickname = body?.Nickname,
                Document = body?.Document,
                Position = body?.Position,
                BirthDate = body?.BirthDate,
                IsActive = body?.IsActive,
                JerseyNumber = body?.JerseyNumber
            },
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{teamId:guid}/players/{playerId:guid}")]
    public async Task<IActionResult> DeletePlayer(
        [FromRoute] Guid leagueId,
        [FromRoute] Guid teamId,
        [FromRoute] Guid playerId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        await _deletePlayerUseCase.ExecuteAsync(
            new DeletePlayerRequest
            {
                LeagueId = leagueId,
                TeamId = teamId,
                PlayerId = playerId,
                UserId = userId
            },
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{teamId:guid}/players/import")]
    public async Task<IActionResult> ImportPlayers(
        [FromRoute] Guid leagueId,
        [FromRoute] Guid teamId,
        [FromBody] ImportPlayersBody body,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId == Guid.Empty) return Unauthorized();

        var response = await _importPlayersUseCase.ExecuteAsync(
            new ImportPlayersRequest
            {
                LeagueId = leagueId,
                TeamId = teamId,
                UserId = userId,
                Players = body?.Players ?? new List<ImportPlayerItem>()
            },
            cancellationToken);

        return Ok(new { createdCount = response.CreatedCount, playerIds = response.PlayerIds });
    }

    private static List<Guid> ParseTeamIds(string? teamIds)
    {
        if (string.IsNullOrWhiteSpace(teamIds))
            return new List<Guid>();

        return teamIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private Guid GetUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Guid.Empty;
        return userId;
    }
}

public sealed class PlayerWriteBody
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? Document { get; set; }
    public string? Position { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool? IsActive { get; set; }
    public int? JerseyNumber { get; set; }
}

public sealed class ImportPlayersBody
{
    public List<ImportPlayerItem> Players { get; set; } = new();
}
