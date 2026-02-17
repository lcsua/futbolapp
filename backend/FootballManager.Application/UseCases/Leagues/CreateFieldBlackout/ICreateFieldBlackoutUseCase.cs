using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateFieldBlackout
{
    public interface ICreateFieldBlackoutUseCase
    {
        Task<CreateFieldBlackoutResponse> ExecuteAsync(CreateFieldBlackoutRequest request, CancellationToken cancellationToken = default);
    }
}
