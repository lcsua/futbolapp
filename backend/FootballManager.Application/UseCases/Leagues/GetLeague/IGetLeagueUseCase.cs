using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.GetLeague;

namespace FootballManager.Application.UseCases.Leagues.GetLeague
{
    public interface IGetLeagueUseCase
    {
        Task<GetLeagueResponse> ExecuteAsync(GetLeagueRequest request, CancellationToken cancellationToken = default);
    }
}
