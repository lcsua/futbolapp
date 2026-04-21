using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.CreateClub
{
    public interface ICreateClubUseCase
    {
        Task<CreateClubResponse> ExecuteAsync(CreateClubRequest request, CancellationToken cancellationToken = default);
    }
}
