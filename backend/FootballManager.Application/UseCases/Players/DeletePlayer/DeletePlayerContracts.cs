using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Players.DeletePlayer;

public interface IDeletePlayerUseCase
{
    Task ExecuteAsync(DeletePlayerRequest request, CancellationToken cancellationToken = default);
}

public sealed class DeletePlayerRequest
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid UserId { get; set; }
}
