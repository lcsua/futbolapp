using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.Interfaces
{
    public interface ILeaguePermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, Guid leagueId, string permissionCode, CancellationToken cancellationToken = default);
        Task<bool> HasAnyPermissionAsync(Guid userId, Guid leagueId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default);
        Task<bool> CanCreateLeagueAsync(Guid userId, CancellationToken cancellationToken = default);
        Task EnsurePermissionAsync(Guid userId, Guid leagueId, string permissionCode, CancellationToken cancellationToken = default);
    }
}
