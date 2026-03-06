using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GenerateSeasonFixtures;

public interface IGenerateSeasonFixturesUseCase
{
    Task<GenerateSeasonFixturesResponse> ExecuteAsync(GenerateSeasonFixturesRequest request, CancellationToken cancellationToken = default);
}
