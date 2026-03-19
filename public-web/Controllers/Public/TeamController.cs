using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Controllers.Public;

[Route("equipo")]
public class TeamController : Controller
{
    private readonly TeamPublicService _teamService;
    private readonly IMemoryCache _cache;

    public TeamController(TeamPublicService teamService, IMemoryCache cache)
    {
        _teamService = teamService;
        _cache = cache;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        string cacheKey = $"equipo_{slug}";
        if (!_cache.TryGetValue(cacheKey, out var model))
        {
            model = await _teamService.GetTeamBySlugAsync(slug);
            if (model == null) return NotFound();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set(cacheKey, model, cacheEntryOptions);
        }

        return View("~/Views/Public/Team.cshtml", model);
    }
}

