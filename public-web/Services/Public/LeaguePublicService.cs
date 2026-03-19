using PublicWeb.Models.Public;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Services.Public;

public class LeaguePublicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LeaguePublicService> _logger;

    public LeaguePublicService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<LeaguePublicService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<LeagueViewModel?> GetLeagueBySlugAsync(string slug)
    {
        string cacheKey = $"liga_{slug}";
        if (_cache.TryGetValue(cacheKey, out LeagueViewModel? model)) return model;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            model = await client.GetFromJsonAsync<LeagueViewModel>($"liga/{slug}");
            if (model != null)
            {
                _cache.Set(cacheKey, model, TimeSpan.FromMinutes(10));
                return model;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for league {Slug}", slug);
        }

        return null;
    }

    public async Task<List<StandingsRowViewModel>> GetStandingsAsync(string leagueSlug)
    {
        string cacheKey = $"tabla_{leagueSlug}";
        if (_cache.TryGetValue(cacheKey, out List<StandingsRowViewModel>? standings)) return standings ?? new();

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            standings = await client.GetFromJsonAsync<List<StandingsRowViewModel>>($"leagues/{leagueSlug}/standings");
            if (standings != null)
            {
                _cache.Set(cacheKey, standings, TimeSpan.FromMinutes(5));
                return standings;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for standings in league {Slug}", leagueSlug);
        }

        return new List<StandingsRowViewModel>();
    }

    public async Task<List<MatchViewModel>> GetResultsAsync(string leagueSlug)
    {
        string cacheKey = $"resultados_{leagueSlug}";
        if (_cache.TryGetValue(cacheKey, out List<MatchViewModel>? results)) return results ?? new();

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            results = await client.GetFromJsonAsync<List<MatchViewModel>>($"leagues/{leagueSlug}/results");
            if (results != null)
            {
                _cache.Set(cacheKey, results, TimeSpan.FromMinutes(5));
                return results;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for results in league {Slug}", leagueSlug);
        }

        return new List<MatchViewModel>();
    }

    public async Task<List<MatchViewModel>> GetFixtureAsync(string leagueSlug)
    {
        string cacheKey = $"fixture_{leagueSlug}";
        if (_cache.TryGetValue(cacheKey, out List<MatchViewModel>? fixture)) return fixture ?? new();

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            fixture = await client.GetFromJsonAsync<List<MatchViewModel>>($"leagues/{leagueSlug}/fixture");
            if (fixture != null)
            {
                _cache.Set(cacheKey, fixture, TimeSpan.FromMinutes(10));
                return fixture;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for fixture in league {Slug}", leagueSlug);
        }

        return new List<MatchViewModel>();
    }
}
