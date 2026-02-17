using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateSeason
{
    public interface ICreateSeasonUseCase
    {
        Task<CreateSeasonResponse> ExecuteAsync(CreateSeasonRequest request, CancellationToken cancellationToken = default);
    }
}
