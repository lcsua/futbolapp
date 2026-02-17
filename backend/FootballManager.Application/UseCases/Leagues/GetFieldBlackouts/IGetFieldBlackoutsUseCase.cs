using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetFieldBlackouts
{
    public interface IGetFieldBlackoutsUseCase
    {
        Task<GetFieldBlackoutsResponse> ExecuteAsync(GetFieldBlackoutsRequest request, CancellationToken cancellationToken = default);
    }
}
