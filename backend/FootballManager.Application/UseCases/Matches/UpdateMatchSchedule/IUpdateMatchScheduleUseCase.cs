using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchSchedule;

public interface IUpdateMatchScheduleUseCase
{
    Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, UpdateMatchScheduleRequest request, CancellationToken cancellationToken = default);
}
