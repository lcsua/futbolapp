using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Roles.DeleteRole
{
    public class DeleteRoleRequest
    {
        public Guid ActorUserId { get; }
        public Guid LeagueId { get; }
        public Guid RoleId { get; }

        public DeleteRoleRequest(Guid actorUserId, Guid leagueId, Guid roleId)
        {
            ActorUserId = actorUserId;
            LeagueId = leagueId;
            RoleId = roleId;
        }
    }

    public interface IDeleteRoleUseCase
    {
        Task ExecuteAsync(DeleteRoleRequest request, CancellationToken cancellationToken = default);
    }

    public class DeleteRoleUseCase : IDeleteRoleUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteRoleUseCase(
            ILeaguePermissionService permissionService,
            IRoleRepository roleRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _roleRepository = roleRepository;
            _userLeagueRepository = userLeagueRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(DeleteRoleRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Roles, cancellationToken);

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException("Role not found.");

            if (role.IsSystem)
                throw new BusinessException("System roles cannot be deleted.");

            if (role.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("That role does not belong to this league.");

            if (await _userLeagueRepository.CountByRoleIdAsync(role.Id, cancellationToken) > 0)
                throw new BusinessException("Cannot delete a role that is assigned to users.");

            _roleRepository.Remove(role);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
