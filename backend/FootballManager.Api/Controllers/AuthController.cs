using FootballManager.Application.UseCases.Auth.Login;
using FootballManager.Application.UseCases.Auth.GetCapabilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ILoginUseCase _loginUseCase;
        private readonly IGetAuthCapabilitiesUseCase _getAuthCapabilitiesUseCase;

        public AuthController(ILoginUseCase loginUseCase, IGetAuthCapabilitiesUseCase getAuthCapabilitiesUseCase)
        {
            _loginUseCase = loginUseCase ?? throw new System.ArgumentNullException(nameof(loginUseCase));
            _getAuthCapabilitiesUseCase = getAuthCapabilitiesUseCase ?? throw new System.ArgumentNullException(nameof(getAuthCapabilitiesUseCase));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            var response = await _loginUseCase.ExecuteAsync(request, cancellationToken);
            if (response == null)
                return Unauthorized();
            return Ok(response);
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var capabilities = await _getAuthCapabilitiesUseCase.ExecuteAsync(userId, cancellationToken);
            return Ok(capabilities);
        }
    }
}
