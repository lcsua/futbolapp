using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.GetMatchById;

public interface IGetMatchByIdUseCase
{
    Task<GetMatchByIdResponse> ExecuteAsync(GetMatchByIdRequest request, CancellationToken cancellationToken = default);
}
