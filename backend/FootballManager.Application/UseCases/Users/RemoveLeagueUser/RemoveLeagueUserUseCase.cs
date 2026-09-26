using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Users.RemoveLeagueUser
{
    public class RemoveLeagueUserRequest
    {
        public Guid ActorUserId { get; }
        public Guid LeagueId { get; }
        public Guid TargetUserId { get; }

        public RemoveLeagueUserRequest(Guid actorUserId, Guid leagueId, Guid targetUserId)
        {
            ActorUserId = actorUserId;
            LeagueId = leagueId;
            TargetUserId = targetUserId;
        }
    }

    public interface IRemoveLeagueUserUseCase
    {
        Task ExecuteAsync(RemoveLeagueUserRequest request, CancellationToken cancellationToken = default);
    }

    public class RemoveLeagueUserUseCase : IRemoveLeagueUserUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveLeagueUserUseCase(
            ILeaguePermissionService permissionService,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _userLeagueRepository = userLeagueRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(RemoveLeagueUserRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);

            if (request.TargetUserId == request.ActorUserId)
                throw new BusinessException("You cannot remove yourself from the league.");

            var membership = await _userLeagueRepository.GetAsync(request.TargetUserId, request.LeagueId, cancellationToken)
                ?? throw new KeyNotFoundException("User is not a member of this league.");

            _userLeagueRepository.Remove(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
