using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Auth.Login
{
    public class LoginUseCase : ILoginUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IDevTokenStore _tokenStore;

        public LoginUseCase(
            IUserRepository userRepository,
            IDevTokenStore tokenStore)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
        }

        public async Task<LoginResponse?> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return null;
            }

            var email = request.Email.Trim();
            var password = request.Password.Trim();
            var user = await _userRepository.GetByEmailAndPasswordAsync(email, password, cancellationToken);
            if (user == null) return null;

            var token = Guid.NewGuid().ToString("N");
            _tokenStore.Register(user.Id, token);

            return new LoginResponse(user.Id, user.Email, user.Role.ToString(), token);
        }
    }
}
