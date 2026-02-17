using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateTeam
{
    public interface ICreateTeamUseCase
    {
        Task<CreateTeamResponse> ExecuteAsync(CreateTeamRequest request, CancellationToken cancellationToken = default);
    }
}
