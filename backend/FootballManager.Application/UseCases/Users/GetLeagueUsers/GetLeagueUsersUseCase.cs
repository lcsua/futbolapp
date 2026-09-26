using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Users.GetLeagueUsers
{
    public class GetLeagueUsersRequest
    {
        public Guid ActorUserId { get; }
        public Guid LeagueId { get; }

        public GetLeagueUsersRequest(Guid actorUserId, Guid leagueId)
        {
            ActorUserId = actorUserId;
            LeagueId = leagueId;
        }
    }

    public record LeagueUserDto(
        Guid UserId,
        string FullName,
        string Email,
        bool IsActive,
        Guid? RoleId,
        string RoleName,
        string? RoleCode,
        bool IsSystemRole);

    public class GetLeagueUsersResponse
    {
        public List<LeagueUserDto> Users { get; }
        public GetLeagueUsersResponse(List<LeagueUserDto> users) => Users = users;
    }

    public interface IGetLeagueUsersUseCase
    {
        Task<GetLeagueUsersResponse> ExecuteAsync(GetLeagueUsersRequest request, CancellationToken cancellationToken = default);
    }

    public class GetLeagueUsersUseCase : IGetLeagueUsersUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueUsersUseCase(
            ILeaguePermissionService permissionService,
            IUserLeagueRepository userLeagueRepository)
        {
            _permissionService = permissionService;
            _userLeagueRepository = userLeagueRepository;
        }

        public async Task<GetLeagueUsersResponse> ExecuteAsync(GetLeagueUsersRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);

            var members = await _userLeagueRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = members.Select(m => new LeagueUserDto(
                m.UserId,
                m.User.FullName,
                m.User.Email,
                m.User.IsActive,
                m.RoleId,
                m.Role?.Name ?? m.AssignedRole.ToString(),
                m.Role?.Code,
                m.Role?.IsSystem ?? false)).ToList();

            return new GetLeagueUsersResponse(dtos);
        }
    }
}
