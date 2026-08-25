using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Users.GetMyAccess
{
    public class GetMyAccessRequest
    {
        public Guid UserId { get; }
        public Guid LeagueId { get; }

        public GetMyAccessRequest(Guid userId, Guid leagueId)
        {
            UserId = userId;
            LeagueId = leagueId;
        }
    }

    public record MyAccessDto(
        Guid LeagueId,
        Guid? RoleId,
        string RoleName,
        string? RoleCode,
        bool IsSystemRole,
        IReadOnlyList<string> Permissions,
        bool CanCreateLeague);

    public interface IGetMyAccessUseCase
    {
        Task<MyAccessDto> ExecuteAsync(GetMyAccessRequest request, CancellationToken cancellationToken = default);
    }

    public class GetMyAccessUseCase : IGetMyAccessUseCase
    {
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly ILeaguePermissionService _permissionService;

        public GetMyAccessUseCase(
            IUserLeagueRepository userLeagueRepository,
            ILeaguePermissionService permissionService)
        {
            _userLeagueRepository = userLeagueRepository;
            _permissionService = permissionService;
        }

        public async Task<MyAccessDto> ExecuteAsync(GetMyAccessRequest request, CancellationToken cancellationToken = default)
        {
            var membership = await _userLeagueRepository.GetWithRoleAsync(request.UserId, request.LeagueId, cancellationToken)
                ?? throw new Exceptions.ForbiddenAccessException("No tenés acceso a esta liga.");

            var permissions = await _permissionService.GetPermissionsAsync(request.UserId, request.LeagueId, cancellationToken);
            var canCreateLeague = await _permissionService.CanCreateLeagueAsync(request.UserId, cancellationToken);

            return new MyAccessDto(
                request.LeagueId,
                membership.RoleId,
                membership.Role?.Name ?? membership.AssignedRole.ToString(),
                membership.Role?.Code,
                membership.Role?.IsSystem ?? false,
                permissions,
                canCreateLeague);
        }
    }
}
