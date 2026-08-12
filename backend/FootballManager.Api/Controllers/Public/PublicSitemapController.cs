using FootballManager.Api.Models.Public;
using FootballManager.Api.Services.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FootballManager.Api.Controllers.Public;

/// <summary>
/// Lean public sitemap payload for the public-web SEO layer.
/// </summary>
[ApiController]
[Route("api/public/sitemap")]
[AllowAnonymous]
public class PublicSitemapController : ControllerBase
{
    private readonly PublicStructuredService _service;

    public PublicSitemapController(PublicStructuredService service)
    {
        _service = service;
    }

    [HttpGet]
    [ResponseCache(Duration = 300)]
    public async Task<ActionResult<SitemapPublicDto>> Get(CancellationToken cancellationToken)
    {
        var dto = await _service.GetSitemapAsync(cancellationToken);
        return Ok(dto);
    }
}
