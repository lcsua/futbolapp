using PublicWeb.Models.Public;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Services.Public;

public class TeamPublicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TeamPublicService> _logger;

    public TeamPublicService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<TeamPublicService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<TeamViewModel?> GetTeamBySlugAsync(string slug)
    {
        string cacheKey = $"equipo_{slug}";
        if (_cache.TryGetValue(cacheKey, out TeamViewModel? model)) return model;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            model = await client.GetFromJsonAsync<TeamViewModel>($"teams/{slug}");
            if (model != null)
            {
                _cache.Set(cacheKey, model, TimeSpan.FromMinutes(10));
                return model;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for team {Slug}", slug);
        }

        return null;
    }
}
