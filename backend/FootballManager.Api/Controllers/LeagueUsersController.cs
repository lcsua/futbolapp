using System.Security.Claims;
using FootballManager.Application.UseCases.Users.CreateLeagueUser;
using FootballManager.Application.UseCases.Users.GetLeagueUsers;
using FootballManager.Application.UseCases.Users.GetMyAccess;
using FootballManager.Application.UseCases.Users.RemoveLeagueUser;
using FootballManager.Application.UseCases.Users.UpdateLeagueUserRole;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    [Route("api/leagues/{leagueId}")]
    public class LeagueUsersController : ControllerBase
    {
        private readonly IGetLeagueUsersUseCase _getLeagueUsersUseCase;
        private readonly ICreateLeagueUserUseCase _createLeagueUserUseCase;
        private readonly IUpdateLeagueUserRoleUseCase _updateLeagueUserRoleUseCase;
        private readonly IRemoveLeagueUserUseCase _removeLeagueUserUseCase;
        private readonly IGetMyAccessUseCase _getMyAccessUseCase;

        public LeagueUsersController(
            IGetLeagueUsersUseCase getLeagueUsersUseCase,
            ICreateLeagueUserUseCase createLeagueUserUseCase,
            IUpdateLeagueUserRoleUseCase updateLeagueUserRoleUseCase,
            IRemoveLeagueUserUseCase removeLeagueUserUseCase,
            IGetMyAccessUseCase getMyAccessUseCase)
        {
            _getLeagueUsersUseCase = getLeagueUsersUseCase;
            _createLeagueUserUseCase = createLeagueUserUseCase;
            _updateLeagueUserRoleUseCase = updateLeagueUserRoleUseCase;
            _removeLeagueUserUseCase = removeLeagueUserUseCase;
            _getMyAccessUseCase = getMyAccessUseCase;
        }

        [HttpGet("my-access")]
        public async Task<IActionResult> GetMyAccess([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getMyAccessUseCase.ExecuteAsync(new GetMyAccessRequest(userId, leagueId), cancellationToken);
            return Ok(response);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var response = await _getLeagueUsersUseCase.ExecuteAsync(new GetLeagueUsersRequest(userId, leagueId), cancellationToken);
            return Ok(response.Users);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser(
            [FromRoute] Guid leagueId,
            [FromBody] CreateLeagueUserBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var request = new CreateLeagueUserRequest
            {
                ActorUserId = userId,
                LeagueId = leagueId,
                FullName = body.FullName,
                Email = body.Email,
                Password = body.Password,
                RoleId = body.RoleId
            };
            var response = await _createLeagueUserUseCase.ExecuteAsync(request, cancellationToken);
            return Ok(response);
        }

        [HttpPut("users/{userId:guid}")]
        public async Task<IActionResult> UpdateUserRole(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid userId,
            [FromBody] UpdateLeagueUserRoleBody body,
            CancellationToken cancellationToken)
        {
            var actorId = GetUserId();
            if (actorId == Guid.Empty) return Unauthorized();

            await _updateLeagueUserRoleUseCase.ExecuteAsync(new UpdateLeagueUserRoleRequest
            {
                ActorUserId = actorId,
                LeagueId = leagueId,
                TargetUserId = userId,
                RoleId = body.RoleId
            }, cancellationToken);
            return NoContent();
        }

        [HttpDelete("users/{userId:guid}")]
        public async Task<IActionResult> RemoveUser(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid userId,
            CancellationToken cancellationToken)
        {
            var actorId = GetUserId();
            if (actorId == Guid.Empty) return Unauthorized();

            await _removeLeagueUserUseCase.ExecuteAsync(new RemoveLeagueUserRequest(actorId, leagueId, userId), cancellationToken);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }
    }

    public class CreateLeagueUserBody
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public Guid RoleId { get; set; }
    }

    public class UpdateLeagueUserRoleBody
    {
        public Guid RoleId { get; set; }
    }
}
