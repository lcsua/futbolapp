using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.AssignDivisionToSeason
{
    public interface IAssignDivisionToSeasonUseCase
    {
        Task<AssignDivisionToSeasonResponse> ExecuteAsync(AssignDivisionToSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
