using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Role>> GetAvailableForLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task<bool> NameExistsAsync(string name, Guid? leagueId, Guid? excludeRoleId, CancellationToken cancellationToken = default);
        Task AddAsync(Role role, CancellationToken cancellationToken = default);
        void Remove(Role role);
    }
}
