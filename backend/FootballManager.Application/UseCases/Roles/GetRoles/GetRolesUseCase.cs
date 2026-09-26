using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Roles.CreateRole;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Roles.GetRoles
{
    public class GetRolesRequest
    {
        public Guid ActorUserId { get; }
        public Guid LeagueId { get; }

        public GetRolesRequest(Guid actorUserId, Guid leagueId)
        {
            ActorUserId = actorUserId;
            LeagueId = leagueId;
        }
    }

    public interface IGetRolesUseCase
    {
        Task<List<RoleDto>> ExecuteAsync(GetRolesRequest request, CancellationToken cancellationToken = default);
    }

    public class GetRolesUseCase : IGetRolesUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IRoleRepository _roleRepository;

        public GetRolesUseCase(ILeaguePermissionService permissionService, IRoleRepository roleRepository)
        {
            _permissionService = permissionService;
            _roleRepository = roleRepository;
        }

        public async Task<List<RoleDto>> ExecuteAsync(GetRolesRequest request, CancellationToken cancellationToken = default)
        {
            var canManage = await _permissionService.HasPermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Roles, cancellationToken);
            var canManageUsers = await _permissionService.HasPermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);
            if (!canManage && !canManageUsers)
                throw new Exceptions.ForbiddenAccessException("No tenés permiso para ver los roles.");

            var roles = await _roleRepository.GetAvailableForLeagueAsync(request.LeagueId, cancellationToken);
            return roles.Select(r => new RoleDto(
                r.Id,
                r.Name,
                r.Description,
                r.Code,
                r.IsSystem,
                r.IsAdminRole
                    ? PermissionCodes.AllCodes
                    : r.RolePermissions.Select(rp => rp.Permission.Code).ToList())).ToList();
        }
    }
}
