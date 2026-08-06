using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteDivision;

public interface IDeleteDivisionUseCase
{
    Task ExecuteAsync(DeleteDivisionRequest request, CancellationToken cancellationToken = default);
}
