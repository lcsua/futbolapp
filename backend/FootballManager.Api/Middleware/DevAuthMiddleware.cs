using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FootballManager.Api.Middleware
{
    public class DevAuthMiddleware
    {
        private const string AuthorizationHeader = "Authorization";
        private const string BearerPrefix = "Bearer ";

        private readonly RequestDelegate _next;

        public DevAuthMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context, IDevTokenStore tokenStore)
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring(BearerPrefix.Length).Trim();
                var userId = tokenStore.GetUserId(token);
                if (userId.HasValue)
                {
                    var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                    }, "Dev");
                    context.User = new ClaimsPrincipal(identity);
                }
            }

            await _next(context);
        }
    }
}
