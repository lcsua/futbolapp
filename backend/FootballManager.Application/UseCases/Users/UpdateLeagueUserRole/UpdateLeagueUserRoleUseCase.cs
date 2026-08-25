using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Users.UpdateLeagueUserRole
{
    public class UpdateLeagueUserRoleRequest
    {
        public Guid ActorUserId { get; set; }
        public Guid LeagueId { get; set; }
        public Guid TargetUserId { get; set; }
        public Guid RoleId { get; set; }
    }

    public interface IUpdateLeagueUserRoleUseCase
    {
        Task ExecuteAsync(UpdateLeagueUserRoleRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateLeagueUserRoleUseCase : IUpdateLeagueUserRoleUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLeagueUserRoleUseCase(
            ILeaguePermissionService permissionService,
            IUserLeagueRepository userLeagueRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _userLeagueRepository = userLeagueRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(UpdateLeagueUserRoleRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);

            var membership = await _userLeagueRepository.GetAsync(request.TargetUserId, request.LeagueId, cancellationToken)
                ?? throw new KeyNotFoundException("User is not a member of this league.");

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException("Role not found.");

            if (role.LeagueId != null && role.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("That role does not belong to this league.");

            if (request.TargetUserId == request.ActorUserId && !role.IsAdminRole)
            {
                var remainingAdmins = (await _userLeagueRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken))
                    .Count(m => m.UserId != request.ActorUserId && (
                        (m.Role != null && m.Role.IsAdminRole) ||
                        (m.Role == null && m.AssignedRole == UserRole.ADMIN)));
                if (remainingAdmins == 0)
                    throw new BusinessException("Cannot remove the last administrator from the league.");
            }

            membership.AssignRole(role);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
