using PublicWeb.Helpers;
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

    public async Task<TeamDetailViewModel?> GetTeamSummaryAsync(
        string leagueSlug,
        string teamSlug,
        string? season = null,
        int nextPage = 1,
        int resultsPage = 1,
        int pageSize = 5)
    {
        var cacheKey = $"equipo_{leagueSlug}_{teamSlug}_{season ?? "active"}_n{nextPage}_r{resultsPage}_p{pageSize}";
        if (_cache.TryGetValue(cacheKey, out TeamDetailViewModel? model) && model != null)
            return model;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var query = new List<string>
            {
                $"nextPage={nextPage}",
                $"resultsPage={resultsPage}",
                $"pageSize={pageSize}"
            };
            if (!string.IsNullOrWhiteSpace(season))
                query.Add($"season={Uri.EscapeDataString(season)}");

            var path = $"liga/{leagueSlug}/equipo/{teamSlug}?{string.Join('&', query)}";
            model = await client.GetFromJsonAsync<TeamDetailViewModel>(path);
            if (model != null)
            {
                var seasonSlug = model.Season?.Slug;
                foreach (var match in model.NextMatches)
                    MatchSlugHelper.ApplySeasonSlug(match, seasonSlug);
                foreach (var match in model.LastResults)
                    MatchSlugHelper.ApplySeasonSlug(match, seasonSlug);
                _cache.Set(cacheKey, model, TimeSpan.FromMinutes(2));
                return model;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for team {League}/{Team}", leagueSlug, teamSlug);
        }

        return null;
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
