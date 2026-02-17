using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateDivision
{
    public interface ICreateDivisionUseCase
    {
        Task<CreateDivisionResponse> ExecuteAsync(CreateDivisionRequest request, CancellationToken cancellationToken = default);
    }
}
