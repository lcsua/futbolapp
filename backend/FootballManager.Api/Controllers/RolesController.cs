using System.Security.Claims;
using FootballManager.Application.UseCases.Roles.CreateRole;
using FootballManager.Application.UseCases.Roles.DeleteRole;
using FootballManager.Application.UseCases.Roles.GetPermissionCatalog;
using FootballManager.Application.UseCases.Roles.GetRoles;
using FootballManager.Application.UseCases.Roles.UpdateRole;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers
{
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IGetRolesUseCase _getRolesUseCase;
        private readonly ICreateRoleUseCase _createRoleUseCase;
        private readonly IUpdateRoleUseCase _updateRoleUseCase;
        private readonly IDeleteRoleUseCase _deleteRoleUseCase;
        private readonly IGetPermissionCatalogUseCase _getPermissionCatalogUseCase;

        public RolesController(
            IGetRolesUseCase getRolesUseCase,
            ICreateRoleUseCase createRoleUseCase,
            IUpdateRoleUseCase updateRoleUseCase,
            IDeleteRoleUseCase deleteRoleUseCase,
            IGetPermissionCatalogUseCase getPermissionCatalogUseCase)
        {
            _getRolesUseCase = getRolesUseCase;
            _createRoleUseCase = createRoleUseCase;
            _updateRoleUseCase = updateRoleUseCase;
            _deleteRoleUseCase = deleteRoleUseCase;
            _getPermissionCatalogUseCase = getPermissionCatalogUseCase;
        }

        [HttpGet("api/permissions")]
        public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var catalog = await _getPermissionCatalogUseCase.ExecuteAsync(cancellationToken);
            return Ok(catalog);
        }

        [HttpGet("api/leagues/{leagueId:guid}/roles")]
        public async Task<IActionResult> GetRoles([FromRoute] Guid leagueId, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var roles = await _getRolesUseCase.ExecuteAsync(new GetRolesRequest(userId, leagueId), cancellationToken);
            return Ok(roles);
        }

        [HttpPost("api/leagues/{leagueId:guid}/roles")]
        public async Task<IActionResult> Create(
            [FromRoute] Guid leagueId,
            [FromBody] SaveRoleBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var role = await _createRoleUseCase.ExecuteAsync(new CreateRoleRequest
            {
                ActorUserId = userId,
                LeagueId = leagueId,
                Name = body.Name,
                Description = body.Description,
                PermissionCodes = body.PermissionCodes ?? []
            }, cancellationToken);
            return Ok(role);
        }

        [HttpPut("api/leagues/{leagueId:guid}/roles/{roleId:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid roleId,
            [FromBody] SaveRoleBody body,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var role = await _updateRoleUseCase.ExecuteAsync(new UpdateRoleRequest
            {
                ActorUserId = userId,
                LeagueId = leagueId,
                RoleId = roleId,
                Name = body.Name,
                Description = body.Description,
                PermissionCodes = body.PermissionCodes ?? []
            }, cancellationToken);
            return Ok(role);
        }

        [HttpDelete("api/leagues/{leagueId:guid}/roles/{roleId:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid leagueId,
            [FromRoute] Guid roleId,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _deleteRoleUseCase.ExecuteAsync(new DeleteRoleRequest(userId, leagueId, roleId), cancellationToken);
            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
        }
    }

    public class SaveRoleBody
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string>? PermissionCodes { get; set; }
    }
}
