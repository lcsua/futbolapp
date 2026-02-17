using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateField
{
    public interface ICreateFieldUseCase
    {
        Task<CreateFieldResponse> ExecuteAsync(CreateFieldRequest request, CancellationToken cancellationToken = default);
    }
}
