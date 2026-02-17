using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetFieldAvailabilities
{
    public interface IGetFieldAvailabilitiesUseCase
    {
        Task<GetFieldAvailabilitiesResponse> ExecuteAsync(GetFieldAvailabilitiesRequest request, CancellationToken cancellationToken = default);
    }
}
