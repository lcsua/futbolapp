using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.GetTeamPlayers;

public interface IGetTeamPlayersUseCase
{
    Task<GetTeamPlayersResponse> ExecuteAsync(GetTeamPlayersRequest request, CancellationToken cancellationToken = default);
}

public sealed class GetTeamPlayersRequest
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
}

public sealed class GetTeamPlayersResponse
{
    public IReadOnlyList<PlayerDto> Players { get; set; } = Array.Empty<PlayerDto>();
}
