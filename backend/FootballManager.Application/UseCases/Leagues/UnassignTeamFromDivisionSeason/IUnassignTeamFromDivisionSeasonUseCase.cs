using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UnassignTeamFromDivisionSeason
{
    public interface IUnassignTeamFromDivisionSeasonUseCase
    {
        Task ExecuteAsync(UnassignTeamFromDivisionSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
