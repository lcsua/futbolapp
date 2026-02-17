using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateLeague
{
    public interface IUpdateLeagueUseCase
    {
        Task ExecuteAsync(UpdateLeagueRequest request, CancellationToken cancellationToken = default);
    }
}
