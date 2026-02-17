using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public interface ICreateLeagueUseCase
    {
        Task<CreateLeagueResponse> ExecuteAsync(CreateLeagueRequest request, CancellationToken cancellationToken = default);
    }
}
