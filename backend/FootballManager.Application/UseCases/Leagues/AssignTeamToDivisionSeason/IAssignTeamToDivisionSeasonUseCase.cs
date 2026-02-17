using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.AssignTeamToDivisionSeason
{
    public interface IAssignTeamToDivisionSeasonUseCase
    {
        Task<AssignTeamToDivisionSeasonResponse> ExecuteAsync(AssignTeamToDivisionSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
