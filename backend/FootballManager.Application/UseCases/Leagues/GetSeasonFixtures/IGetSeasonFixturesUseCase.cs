using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;

public interface IGetSeasonFixturesUseCase
{
    Task<GetSeasonFixturesResponse?> ExecuteAsync(GetSeasonFixturesRequest request, CancellationToken cancellationToken = default);
}
