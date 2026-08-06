using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.ReopenSeason;

public interface IReopenSeasonUseCase
{
    Task ExecuteAsync(ReopenSeasonRequest request, CancellationToken cancellationToken = default);
}
