using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.SeedLeagueDocumentDefaults
{
    public interface ISeedLeagueDocumentDefaultsUseCase
    {
        Task<SeedLeagueDocumentDefaultsResponse> ExecuteAsync(SeedLeagueDocumentDefaultsRequest request, CancellationToken cancellationToken = default);
    }
}
