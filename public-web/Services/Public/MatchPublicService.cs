using PublicWeb.Models.Public;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace PublicWeb.Services.Public;

public class MatchPublicService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MatchPublicService> _logger;

    public MatchPublicService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<MatchPublicService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<MatchViewModel?> GetMatchByIdAsync(Guid id)
    {
        string cacheKey = $"partido_{id}";
        if (_cache.TryGetValue(cacheKey, out MatchViewModel? model)) return model;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            model = await client.GetFromJsonAsync<MatchViewModel>($"matches/{id}");
            if (model != null)
            {
                _cache.Set(cacheKey, model, TimeSpan.FromMinutes(5));
                return model;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for match {Id}", id);
        }

        return null;
    }
}
