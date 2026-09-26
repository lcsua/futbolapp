using System.Security.Claims;
using FootballManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FootballManager.Api.Authorization
{
    public class LeaguePermissionFilter : IAsyncActionFilter
    {
        private readonly ILeaguePermissionService _permissionService;

        public LeaguePermissionFilter(ILeaguePermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var path = http.Request.Path.Value ?? string.Empty;
            var method = http.Request.Method;
            var requirement = LeagueRoutePermissionResolver.Resolve(method, path);

            if (requirement == null)
            {
                await next();
                return;
            }

            var userIdString = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                await next();
                return;
            }

            if (!TryGetLeagueId(context, path, out var leagueId))
            {
                if (requirement.AnyOf.Contains("leagues") && path.StartsWith("/api/leagues", StringComparison.OrdinalIgnoreCase))
                {
                    if (!await _permissionService.CanCreateLeagueAsync(userId, http.RequestAborted))
                    {
                        context.Result = new JsonResult(new { error = "No tenés permiso para realizar esta acción." })
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };
                        return;
                    }
                }

                await next();
                return;
            }

            var allowed = await _permissionService.HasAnyPermissionAsync(userId, leagueId, requirement.AnyOf, http.RequestAborted);
            if (!allowed)
            {
                context.Result = new JsonResult(new { error = "No tenés permiso para realizar esta acción." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }

        private static bool TryGetLeagueId(ActionExecutingContext context, string path, out Guid leagueId)
        {
            leagueId = Guid.Empty;
            if (context.RouteData.Values.TryGetValue("leagueId", out var raw) && raw != null && Guid.TryParse(raw.ToString(), out leagueId))
                return true;

            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts[0].Equals("api", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("leagues", StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(parts[2], out leagueId))
                return true;

            return false;
        }
    }
}
