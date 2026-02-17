using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IUserLeagueRepository
    {
        Task<bool> IsUserInLeagueAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(UserLeague userLeague, CancellationToken cancellationToken = default);
    }
}
