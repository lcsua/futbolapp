using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateField
{
    public interface IUpdateFieldUseCase
    {
        Task ExecuteAsync(UpdateFieldRequest request, CancellationToken cancellationToken = default);
    }
}
