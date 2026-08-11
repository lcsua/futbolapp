using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Players.GetTeamPlayers;

public sealed class GetTeamPlayersUseCase : IGetTeamPlayersUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;

    public GetTeamPlayersUseCase(
        IUserLeagueRepository userLeagueRepository,
        ITeamRepository teamRepository,
        IPlayerRepository playerRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public async Task<GetTeamPlayersResponse> ExecuteAsync(GetTeamPlayersRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team == null || team.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Team {request.TeamId} not found in league {request.LeagueId}.");

        var players = await _playerRepository.GetByTeamIdAsync(request.TeamId, cancellationToken);
        return new GetTeamPlayersResponse
        {
            Players = players.Select(PlayerMapping.ToDto).ToList()
        };
    }
}
