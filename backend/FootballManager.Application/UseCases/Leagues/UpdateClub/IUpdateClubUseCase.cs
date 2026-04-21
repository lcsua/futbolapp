using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpdateClub
{
    public interface IUpdateClubUseCase
    {
        Task ExecuteAsync(UpdateClubRequest request, CancellationToken cancellationToken = default);
    }
}
