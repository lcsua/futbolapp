using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Services
{
    public class LeaguePermissionService : ILeaguePermissionService
    {
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUserRepository _userRepository;

        public LeaguePermissionService(
            IUserLeagueRepository userLeagueRepository,
            IUserRepository userRepository)
        {
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<bool> HasPermissionAsync(Guid userId, Guid leagueId, string permissionCode, CancellationToken cancellationToken = default)
        {
            var permissions = await GetPermissionsAsync(userId, leagueId, cancellationToken);
            return permissions.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> HasAnyPermissionAsync(Guid userId, Guid leagueId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default)
        {
            var permissions = await GetPermissionsAsync(userId, leagueId, cancellationToken);
            return permissionCodes.Any(code => permissions.Contains(code, StringComparer.OrdinalIgnoreCase));
        }

        public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
        {
            var membership = await _userLeagueRepository.GetWithRoleAsync(userId, leagueId, cancellationToken);
            if (membership == null) return Array.Empty<string>();

            if (membership.Role != null && membership.Role.IsAdminRole)
                return PermissionCodes.AllCodes;

            if (membership.Role == null && membership.AssignedRole == UserRole.ADMIN)
                return PermissionCodes.AllCodes;

            if (membership.Role == null)
                return Array.Empty<string>();

            return membership.Role.RolePermissions
                .Select(rp => rp.Permission.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<bool> CanCreateLeagueAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;
            if (user.Role == UserRole.ADMIN) return true;
            return await _userLeagueRepository.HasPermissionInAnyLeagueAsync(userId, PermissionCodes.Leagues, cancellationToken);
        }

        public async Task EnsurePermissionAsync(Guid userId, Guid leagueId, string permissionCode, CancellationToken cancellationToken = default)
        {
            if (!await HasPermissionAsync(userId, leagueId, permissionCode, cancellationToken))
                throw new ForbiddenAccessException("No tenés permiso para realizar esta acción.");
        }
    }
}
