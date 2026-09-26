using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IUserLeagueRepository
    {
        Task<bool> IsUserInLeagueAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(UserLeague userLeague, CancellationToken cancellationToken = default);
        Task<UserLeague?> GetAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default);
        Task<UserLeague?> GetWithRoleAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default);
        Task<List<UserLeague>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task<bool> HasPermissionInAnyLeagueAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
        Task<int> CountByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
        void Remove(UserLeague userLeague);
    }
}
