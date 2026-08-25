using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Users.CreateLeagueUser
{
    public class CreateLeagueUserRequest
    {
        public Guid ActorUserId { get; set; }
        public Guid LeagueId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public Guid RoleId { get; set; }
    }

    public record CreateLeagueUserResponse(
        Guid UserId,
        string FullName,
        string Email,
        Guid RoleId,
        string RoleName,
        bool CreatedUser);

    public interface ICreateLeagueUserUseCase
    {
        Task<CreateLeagueUserResponse> ExecuteAsync(CreateLeagueUserRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateLeagueUserUseCase : ICreateLeagueUserUseCase
    {
        private readonly ILeaguePermissionService _permissionService;
        private readonly IUserRepository _userRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly ILeagueRepository _leagueRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateLeagueUserUseCase(
            ILeaguePermissionService permissionService,
            IUserRepository userRepository,
            IUserLeagueRepository userLeagueRepository,
            ILeagueRepository leagueRepository,
            IRoleRepository roleRepository,
            IUnitOfWork unitOfWork)
        {
            _permissionService = permissionService;
            _userRepository = userRepository;
            _userLeagueRepository = userLeagueRepository;
            _leagueRepository = leagueRepository;
            _roleRepository = roleRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateLeagueUserResponse> ExecuteAsync(CreateLeagueUserRequest request, CancellationToken cancellationToken = default)
        {
            await _permissionService.EnsurePermissionAsync(request.ActorUserId, request.LeagueId, PermissionCodes.Users, cancellationToken);

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email required.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken)
                ?? throw new KeyNotFoundException("League not found.");

            var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken)
                ?? throw new KeyNotFoundException("Role not found.");

            if (role.LeagueId != null && role.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("That role does not belong to this league.");

            var email = request.Email.Trim();
            var existing = await _userRepository.GetByEmailAsync(email, cancellationToken);
            var createdUser = false;
            User user;

            if (existing == null)
            {
                if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Trim().Length < 6)
                    throw new ArgumentException("Password must be at least 6 characters.");
                if (string.IsNullOrWhiteSpace(request.FullName))
                    throw new ArgumentException("Full name required.");

                user = new User(request.FullName.Trim(), email);
                user.AssignRole(UserRole.USER);
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _userRepository.SetPasswordAsync(user.Id, request.Password.Trim(), cancellationToken);
                createdUser = true;
            }
            else
            {
                user = existing;
                var alreadyMember = await _userLeagueRepository.GetAsync(user.Id, request.LeagueId, cancellationToken);
                if (alreadyMember != null)
                    throw new BusinessException("That user already belongs to this league.");
            }

            var membership = new UserLeague(user, league, role);
            await _userLeagueRepository.AddAsync(membership, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateLeagueUserResponse(user.Id, user.FullName, user.Email, role.Id, role.Name, createdUser);
        }
    }
}
