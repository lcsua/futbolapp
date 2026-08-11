using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Players.GetPlayersByTeamIds;

public sealed class GetPlayersByTeamIdsUseCase : IGetPlayersByTeamIdsUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;

    public GetPlayersByTeamIdsUseCase(
        IUserLeagueRepository userLeagueRepository,
        ITeamRepository teamRepository,
        IPlayerRepository playerRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
    }

    public async Task<GetPlayersByTeamIdsResponse> ExecuteAsync(GetPlayersByTeamIdsRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var teamIds = request.TeamIds?.Where(id => id != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
        if (teamIds.Count == 0)
            return new GetPlayersByTeamIdsResponse();

        var leagueTeams = await _teamRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var leagueTeamIds = leagueTeams.Select(t => t.Id).ToHashSet();
        var validIds = teamIds.Where(leagueTeamIds.Contains).ToList();
        if (validIds.Count == 0)
            return new GetPlayersByTeamIdsResponse();

        var players = await _playerRepository.GetByTeamIdsAsync(validIds, cancellationToken);
        return new GetPlayersByTeamIdsResponse
        {
            Players = players.Select(PlayerMapping.ToDto).ToList()
        };
    }
}
