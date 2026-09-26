using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Roles.CreateRole
{
    public class CreateRoleRequest
    {
        public Guid ActorUserId { get; set; }
        public Guid LeagueId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> PermissionCodes { get; set; } = [];
    }

    public record RoleDto(
        Guid Id,
        string Name,
        string Description,
        string? Code,
        bool IsSystem,
        IReadOnlyList<string> Permissions);

    public interface ICreateRoleUseCase
    {
        Task<RoleDto> ExecuteAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateRoleUseCase : ICreateRoleUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateRoleUseCase(
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

        public async Task<RoleDto> ExecuteAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Roles, cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Role name required.");

            if (await _roleRepository.NameExistsAsync(request.Name, request.LeagueId, null, cancellationToken))
                throw new BusinessException("A role with that name already exists in this league.");

            var permissions = await _permissionRepository.GetByCodesAsync(request.PermissionCodes ?? [], cancellationToken);
            var role = new Role(request.Name, request.Description, request.LeagueId);
            role.ReplacePermissions(permissions);

            await _roleRepository.AddAsync(role, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RoleDto(role.Id, role.Name, role.Description, role.Code, role.IsSystem,
                permissions.Select(p => p.Code).ToList());
        }
    }
}
