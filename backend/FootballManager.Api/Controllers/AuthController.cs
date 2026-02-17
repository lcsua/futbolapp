using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Auth.Login;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginUseCase _loginUseCase;

        public AuthController(ILoginUseCase loginUseCase)
        {
            _loginUseCase = loginUseCase ?? throw new System.ArgumentNullException(nameof(loginUseCase));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var response = await _loginUseCase.ExecuteAsync(request, cancellationToken);
            if (response == null)
                return Unauthorized();
            return Ok(response);
        }
    }
}
