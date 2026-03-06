using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchResult;

public interface IUpdateMatchResultUseCase
{
    Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, UpdateMatchResultRequest request, CancellationToken cancellationToken = default);
}
