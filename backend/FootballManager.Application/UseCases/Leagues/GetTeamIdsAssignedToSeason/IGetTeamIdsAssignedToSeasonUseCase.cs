using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetTeamIdsAssignedToSeason
{
    public interface IGetTeamIdsAssignedToSeasonUseCase
    {
        Task<GetTeamIdsAssignedToSeasonResponse> ExecuteAsync(GetTeamIdsAssignedToSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
