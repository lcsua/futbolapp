using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.GetMatches;

public interface IGetMatchesUseCase
{
    Task<GetMatchesResponse> ExecuteAsync(GetMatchesRequest request, CancellationToken cancellationToken = default);
}
