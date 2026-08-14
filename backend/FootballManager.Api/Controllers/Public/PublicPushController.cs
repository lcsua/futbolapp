using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Push;
using FootballManager.Domain.Enums;
using FootballManager.Infrastructure.Push;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace FootballManager.Api.Controllers.Public;

[ApiController]
[Route("api/public/push")]
[AllowAnonymous]
[EnableRateLimiting("push")]
public class PublicPushController : ControllerBase
{
    private readonly IPushSubscriptionService _subscriptions;
    private readonly WebPushOptions _options;

    public PublicPushController(IPushSubscriptionService subscriptions, IOptions<WebPushOptions> options)
    {
        _subscriptions = subscriptions;
        _options = options.Value;
    }

    [HttpGet("vapid-public-key")]
    public IActionResult GetVapidPublicKey()
    {
        if (string.IsNullOrWhiteSpace(_options.PublicKey))
            return NotFound(new { message = "Web Push is not configured." });
        return Ok(new { publicKey = _options.PublicKey });
    }

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest(new { message = "Body required." });
        try
        {
            await _subscriptions.UpsertSubscriptionAsync(
                request.Endpoint,
                request.P256dh,
                request.Auth,
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            return Ok(new { ok = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("follow")]
    public async Task<IActionResult> Follow([FromBody] PushFollowRequest request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest(new { message = "Body required." });
        if (!TryParseScope(request.ScopeType, out var scopeType))
            return BadRequest(new { message = "Unsupported scopeType." });
        if (request.ScopeId == Guid.Empty)
            return BadRequest(new { message = "scopeId is required." });

        try
        {
            if (!string.IsNullOrWhiteSpace(request.P256dh) && !string.IsNullOrWhiteSpace(request.Auth))
            {
                await _subscriptions.UpsertSubscriptionAsync(
                    request.Endpoint,
                    request.P256dh,
                    request.Auth,
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);
            }

            await _subscriptions.FollowAsync(request.Endpoint, scopeType, request.ScopeId, cancellationToken);
            return Ok(new { following = true });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("follow")]
    public async Task<IActionResult> Unfollow([FromBody] PushUnfollowRequest request, CancellationToken cancellationToken)
    {
        if (request == null) return BadRequest(new { message = "Body required." });
        if (!TryParseScope(request.ScopeType, out var scopeType))
            return BadRequest(new { message = "Unsupported scopeType." });

        await _subscriptions.UnfollowAsync(request.Endpoint, scopeType, request.ScopeId, cancellationToken);
        return Ok(new { following = false });
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status(
        [FromQuery] string endpoint,
        [FromQuery] string scopeType,
        [FromQuery] Guid scopeId,
        CancellationToken cancellationToken)
    {
        if (!TryParseScope(scopeType, out var parsed))
            return BadRequest(new { message = "Unsupported scopeType." });

        var following = await _subscriptions.IsFollowingAsync(endpoint, parsed, scopeId, cancellationToken);
        return Ok(new { following });
    }

    private static bool TryParseScope(string? raw, out PushFollowScopeType scopeType)
    {
        scopeType = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return Enum.TryParse(raw, ignoreCase: true, out scopeType)
               && (scopeType is PushFollowScopeType.League or PushFollowScopeType.Team);
    }
}

public sealed class PushSubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public sealed class PushFollowRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string? P256dh { get; set; }
    public string? Auth { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
}

public sealed class PushUnfollowRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public Guid ScopeId { get; set; }
}
