using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.ShiftFixtureDates;

public interface IShiftFixtureDatesUseCase
{
    Task<ShiftFixtureDatesResponse> ExecuteAsync(
        ShiftFixtureDatesRequest request,
        CancellationToken cancellationToken = default);
}
