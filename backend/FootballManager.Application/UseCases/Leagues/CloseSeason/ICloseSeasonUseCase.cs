using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CloseSeason;

public interface ICloseSeasonUseCase
{
    Task<CloseSeasonResponse> ExecuteAsync(CloseSeasonRequest request, CancellationToken cancellationToken = default);
}
