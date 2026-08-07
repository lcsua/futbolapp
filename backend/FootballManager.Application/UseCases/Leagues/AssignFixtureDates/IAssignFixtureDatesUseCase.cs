using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.AssignFixtureDates;

public interface IAssignFixtureDatesUseCase
{
    Task<AssignFixtureDatesResponse> ExecuteAsync(
        AssignFixtureDatesRequest request,
        CancellationToken cancellationToken = default);
}
