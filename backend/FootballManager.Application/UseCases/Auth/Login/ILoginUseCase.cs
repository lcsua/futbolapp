using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Auth.Login
{
    public interface ILoginUseCase
    {
        /// <summary>
        /// Returns login response if email is allowed; null otherwise.
        /// </summary>
        Task<LoginResponse?> ExecuteAsync(LoginRequest request, CancellationToken cancellationToken = default);
    }
}
