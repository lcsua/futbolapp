using FootballManager.Application.Interfaces;

namespace FootballManager.Application.UseCases.Auth.GetCapabilities
{
    public record AuthCapabilitiesDto(Guid UserId, string Email, string Role, bool CanCreateLeague);

    public interface IGetAuthCapabilitiesUseCase
    {
        Task<AuthCapabilitiesDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
    }

    public class GetAuthCapabilitiesUseCase : IGetAuthCapabilitiesUseCase
    {
        private readonly Interfaces.Repositories.IUserRepository _userRepository;
        private readonly ILeaguePermissionService _permissionService;

        public GetAuthCapabilitiesUseCase(
            Interfaces.Repositories.IUserRepository userRepository,
            ILeaguePermissionService permissionService)
        {
            _userRepository = userRepository;
            _permissionService = permissionService;
        }

        public async Task<AuthCapabilitiesDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("User not found.");

            var canCreate = await _permissionService.CanCreateLeagueAsync(userId, cancellationToken);
            return new AuthCapabilitiesDto(user.Id, user.Email, user.Role.ToString(), canCreate);
        }
    }
}
