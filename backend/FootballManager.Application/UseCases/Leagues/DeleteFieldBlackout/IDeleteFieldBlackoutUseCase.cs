using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteFieldBlackout
{
    public interface IDeleteFieldBlackoutUseCase
    {
        Task ExecuteAsync(DeleteFieldBlackoutRequest request, CancellationToken cancellationToken = default);
    }
}
