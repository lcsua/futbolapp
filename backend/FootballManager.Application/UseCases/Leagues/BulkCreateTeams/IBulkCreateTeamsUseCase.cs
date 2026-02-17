using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.BulkCreateTeams
{
    public interface IBulkCreateTeamsUseCase
    {
        Task<BulkCreateTeamsResponse> ExecuteAsync(BulkCreateTeamsRequest request, CancellationToken cancellationToken = default);
    }
}
