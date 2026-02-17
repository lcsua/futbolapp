using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueDivisions
{
    public interface IGetLeagueDivisionsUseCase
    {
        Task<GetLeagueDivisionsResponse> ExecuteAsync(GetLeagueDivisionsRequest request, CancellationToken cancellationToken = default);
    }
}
