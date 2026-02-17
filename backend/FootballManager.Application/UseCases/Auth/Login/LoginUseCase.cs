using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Auth.Login
{
    public class LoginUseCase : ILoginUseCase
    {
        public const string SuperuserEmail = "lucasmbolivar@gmail.com";
        public const string SuperuserRoleName = "SUPER_ADMIN";

        private readonly IUserRepository _userRepository;
        private readonly IDevTokenStore _tokenStore;
        private readonly IUnitOfWork _unitOfWork;

        public LoginUseCase(
            IUserRepository userRepository,
            IDevTokenStore tokenStore,
            IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _tokenStore = tokenStore ?? throw new ArgumentNullException(nameof(tokenStore));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<LoginResponse?> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                !string.Equals(request.Email.Trim(), SuperuserEmail, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var email = request.Email.Trim();
            var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

            if (user == null)
            {
                // Dev-only user: password_hash is NOT NULL in the database, so store a placeholder.
                // This superuser authenticates via the dev token, not via password or Google.
                user = new User(
                    "Dev Superuser",
                    email,
                    passwordHash: "DEV_SUPERUSER_NO_PASSWORD",
                    googleSub: "DEV_SUPERUSER_NO_GOOGLE");
                user.AssignRole(UserRole.ADMIN);
                await _userRepository.AddAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var token = Guid.NewGuid().ToString("N");
            _tokenStore.Register(user.Id, token);

            return new LoginResponse(user.Id, user.Email, SuperuserRoleName, token);
        }
    }
}
