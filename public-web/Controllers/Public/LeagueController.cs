using PublicWeb.Services.Public;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Controllers.Public;

[Route("liga")]
public class LeagueController : Controller
{
    private readonly LeaguePublicService _leagueService;
    private readonly IMemoryCache _cache;

    public LeagueController(LeaguePublicService leagueService, IMemoryCache cache)
    {
        _leagueService = leagueService;
        _cache = cache;
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Index(string slug)
    {
        string cacheKey = $"liga_{slug}";
        if (!_cache.TryGetValue(cacheKey, out var model))
        {
            model = await _leagueService.GetLeagueBySlugAsync(slug);
            if (model == null) return NotFound();

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set(cacheKey, model, cacheEntryOptions);
        }

        return View("~/Views/Public/League.cshtml", model);
    }

    [HttpGet("{slug}/tabla")]
    public async Task<IActionResult> Standings(string slug)
    {
        string cacheKey = $"tabla_{slug}";
        if (!_cache.TryGetValue(cacheKey, out var standings))
        {
            var league = await _leagueService.GetLeagueBySlugAsync(slug);
            if (league == null) return NotFound();

            standings = await _leagueService.GetStandingsAsync(slug);
            ViewBag.League = league;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, standings, cacheEntryOptions);
        }

        return View("~/Views/Public/Standings.cshtml", standings);
    }

    [HttpGet("{slug}/resultados")]
    public async Task<IActionResult> Results(string slug)
    {
        string cacheKey = $"resultados_{slug}";
        if (!_cache.TryGetValue(cacheKey, out var results))
        {
            var league = await _leagueService.GetLeagueBySlugAsync(slug);
            if (league == null) return NotFound();

            results = await _leagueService.GetResultsAsync(slug);
            ViewBag.League = league;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            _cache.Set(cacheKey, results, cacheEntryOptions);
        }

        return View("~/Views/Public/Results.cshtml", results);
    }

    [HttpGet("{slug}/fixture")]
    public async Task<IActionResult> Fixture(string slug)
    {
        string cacheKey = $"fixture_{slug}";
        if (!_cache.TryGetValue(cacheKey, out var fixture))
        {
            var league = await _leagueService.GetLeagueBySlugAsync(slug);
            if (league == null) return NotFound();

            fixture = await _leagueService.GetFixtureAsync(slug);
            ViewBag.League = league;

            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
            _cache.Set(cacheKey, fixture, cacheEntryOptions);
        }

        return View("~/Views/Public/Fixture.cshtml", fixture);
    }
}

