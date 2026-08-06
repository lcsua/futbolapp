using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteSeason;

public interface IDeleteSeasonUseCase
{
    Task ExecuteAsync(DeleteSeasonRequest request, CancellationToken cancellationToken = default);
}
