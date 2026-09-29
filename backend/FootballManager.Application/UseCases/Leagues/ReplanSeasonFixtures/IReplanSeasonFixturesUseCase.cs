using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.ReplanSeasonFixtures;

public interface IReplanSeasonFixturesUseCase
{
    Task<FixtureDraftDto> ExecuteAsync(ReplanSeasonFixturesRequest request, CancellationToken cancellationToken = default);
}
