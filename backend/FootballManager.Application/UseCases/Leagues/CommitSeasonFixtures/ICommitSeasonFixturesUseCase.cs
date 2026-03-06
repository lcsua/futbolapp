using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CommitSeasonFixtures;

public interface ICommitSeasonFixturesUseCase
{
    Task ExecuteAsync(CommitSeasonFixturesRequest request, CancellationToken cancellationToken = default);
}
