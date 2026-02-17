using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateSeason
{
    public interface IUpdateSeasonUseCase
    {
        Task ExecuteAsync(UpdateSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
