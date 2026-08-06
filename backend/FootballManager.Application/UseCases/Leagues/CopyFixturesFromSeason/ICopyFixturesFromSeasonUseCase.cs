using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CopyFixturesFromSeason;

public interface ICopyFixturesFromSeasonUseCase
{
    Task<CopyFixturesFromSeasonResponse> ExecuteAsync(
        CopyFixturesFromSeasonRequest request,
        CancellationToken cancellationToken = default);
}
