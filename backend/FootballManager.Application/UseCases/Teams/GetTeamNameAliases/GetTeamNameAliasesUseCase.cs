using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Teams.GetTeamNameAliases;

public interface IGetTeamNameAliasesUseCase
{
    Task<GetTeamNameAliasesResponse> ExecuteAsync(
        GetTeamNameAliasesRequest request,
        CancellationToken cancellationToken = default);
}

public class GetTeamNameAliasesRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
}

public class TeamNameAliasDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string NormalizedAlias { get; set; } = string.Empty;
}

public class GetTeamNameAliasesResponse
{
    public IReadOnlyList<TeamNameAliasDto> Items { get; }

    public GetTeamNameAliasesResponse(IReadOnlyList<TeamNameAliasDto> items)
    {
        Items = items;
    }
}

public sealed class GetTeamNameAliasesUseCase : IGetTeamNameAliasesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamNameAliasRepository _aliasRepository;

    public GetTeamNameAliasesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ITeamNameAliasRepository aliasRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _aliasRepository = aliasRepository ?? throw new ArgumentNullException(nameof(aliasRepository));
    }

    public async Task<GetTeamNameAliasesResponse> ExecuteAsync(
        GetTeamNameAliasesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var items = await _aliasRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        return new GetTeamNameAliasesResponse(items.Select(a => new TeamNameAliasDto
        {
            Id = a.Id,
            TeamId = a.TeamId,
            Alias = a.Alias,
            NormalizedAlias = a.NormalizedAlias,
        }).ToList());
    }
}
