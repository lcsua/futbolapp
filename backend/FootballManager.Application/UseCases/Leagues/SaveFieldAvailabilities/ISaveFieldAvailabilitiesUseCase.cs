using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.SaveFieldAvailabilities
{
    public interface ISaveFieldAvailabilitiesUseCase
    {
        Task ExecuteAsync(SaveFieldAvailabilitiesRequest request, CancellationToken cancellationToken = default);
    }
}
