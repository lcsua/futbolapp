using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Roles.CreateRole;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Roles.UpdateRole
{
    public class UpdateRoleRequest
    {
        public Guid ActorUserId { get; set; }
        public Guid LeagueId { get; set; }
        public Guid RoleId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> PermissionCodes { get; set; } = [];
    }

    public interface IUpdateRoleUseCase
    {
        Task<RoleDto> ExecuteAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateRoleUseCase : IUpdateRoleUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateRoleUseCase(
            ILeaguePermissionService permissionService,
            IRoleRepository roleRepository,
            IPermissionRepository permissionRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<RoleDto> ExecuteAsync(UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Roles, cancellationToken);

            var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException("Role not found.");

            if (role.LeagueId != null && role.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("That role does not belong to this league.");

            if (role.IsAdminRole)
                throw new BusinessException("The administrator role cannot be edited.");

            if (role.IsSystem && role.LeagueId == null && role.Code != RoleCodes.Carga)
            {
                // system roles other than Carga stay locked
            }

            if (await _roleRepository.NameExistsAsync(request.Name, role.LeagueId, role.Id, cancellationToken))
                throw new BusinessException("A role with that name already exists.");

            if (!role.IsSystem)
                role.UpdateDetails(request.Name, request.Description);
            else
                role.UpdateDetails(role.Name, request.Description);

            var permissions = await _permissionRepository.GetByCodesAsync(request.PermissionCodes ?? [], cancellationToken);
            role.ReplacePermissions(permissions);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RoleDto(role.Id, role.Name, role.Description, role.Code, role.IsSystem,
                permissions.Select(p => p.Code).ToList());
        }
    }
}
