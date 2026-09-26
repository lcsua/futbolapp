using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;

namespace FootballManager.Application.UseCases.Users.SetLeagueUserPassword
{
    public class SetLeagueUserPasswordRequest
    {
        public Guid ActorUserId { get; set; }
        public Guid LeagueId { get; set; }
        public Guid TargetUserId { get; set; }
        public string Password { get; set; } = string.Empty;
    }

    public interface ISetLeagueUserPasswordUseCase
    {
        Task ExecuteAsync(SetLeagueUserPasswordRequest request, CancellationToken cancellationToken = default);
    }

    public class SetLeagueUserPasswordUseCase : ISetLeagueUserPasswordUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetLeagueUserPasswordUseCase(
            ILeaguePermissionService permissionService,
            IUserLeagueRepository userLeagueRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _userLeagueRepository = userLeagueRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(SetLeagueUserPasswordRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
                throw new ArgumentException("Password must be at least 6 characters.");

            _ = await _userLeagueRepository.GetAsync(request.TargetUserId, request.LeagueId, cancellationToken)
                ?? throw new KeyNotFoundException("User is not a member of this league.");

            await _userRepository.SetPasswordAsync(request.TargetUserId, request.Password.Trim(), cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
