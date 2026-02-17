using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateDivision
{
    public interface IUpdateDivisionUseCase
    {
        Task ExecuteAsync(UpdateDivisionRequest request, CancellationToken cancellationToken = default);
    }
}
