using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.GetPlayersByTeamIds;

public interface IGetPlayersByTeamIdsUseCase
{
    Task<GetPlayersByTeamIdsResponse> ExecuteAsync(GetPlayersByTeamIdsRequest request, CancellationToken cancellationToken = default);
}

public sealed class GetPlayersByTeamIdsRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public IReadOnlyList<Guid> TeamIds { get; set; } = Array.Empty<Guid>();
}

public sealed class GetPlayersByTeamIdsResponse
{
    public IReadOnlyList<PlayerDto> Players { get; set; } = Array.Empty<PlayerDto>();
}
