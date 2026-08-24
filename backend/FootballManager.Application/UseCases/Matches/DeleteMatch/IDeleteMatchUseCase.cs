using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.DeleteMatch;

public interface IDeleteMatchUseCase
{
    Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, CancellationToken cancellationToken = default);
}
