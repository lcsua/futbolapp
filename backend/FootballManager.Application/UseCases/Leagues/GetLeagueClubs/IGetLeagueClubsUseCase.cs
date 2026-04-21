using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueClubs
{
    public interface IGetLeagueClubsUseCase
    {
        Task<GetLeagueClubsResponse> ExecuteAsync(GetLeagueClubsRequest request, CancellationToken cancellationToken = default);
    }
}
