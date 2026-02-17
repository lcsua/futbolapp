using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateTeam
{
    public interface IUpdateTeamUseCase
    {
        Task ExecuteAsync(UpdateTeamRequest request, CancellationToken cancellationToken = default);
    }
}
