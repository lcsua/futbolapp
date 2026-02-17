using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueFields
{
    public interface IGetLeagueFieldsUseCase
    {
        Task<GetLeagueFieldsResponse> ExecuteAsync(GetLeagueFieldsRequest request, CancellationToken cancellationToken = default);
    }
}
