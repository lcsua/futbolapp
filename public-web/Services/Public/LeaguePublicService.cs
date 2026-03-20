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

    public async Task<List<SeasonViewModel>> GetLeagueMetaAsync(string slug)
    {
        string cacheKey = $"liga_meta_{slug}";
        if (_cache.TryGetValue(cacheKey, out List<SeasonViewModel>? meta)) return meta ?? new();

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            meta = await client.GetFromJsonAsync<List<SeasonViewModel>>($"liga/{slug}/meta");
            if (meta != null)
            {
                _cache.Set(cacheKey, meta, TimeSpan.FromMinutes(15));
                return meta;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling backend API for meta in league {Slug}", slug);
        }

        return new();
    }

    public async Task<SeasonGroupedViewModel<StandingsRowViewModel>?> GetStandingsAsync(string leagueSlug, string? seasonSlug, string? divisionSlug)
    {
        string querySeason = string.IsNullOrWhiteSpace(seasonSlug) ? "" : $"season={seasonSlug}";
        string queryDiv = string.IsNullOrWhiteSpace(divisionSlug) || divisionSlug == "all" ? "" : $"division={divisionSlug}";
        string query = string.Join("&", new[] { querySeason, queryDiv }.Where(q => !string.IsNullOrEmpty(q)));
        query = string.IsNullOrEmpty(query) ? "" : "?" + query;

        string cacheKey = $"tabla_{leagueSlug}_{seasonSlug ?? "default"}_{divisionSlug ?? "all"}";
        if (_cache.TryGetValue(cacheKey, out SeasonGroupedViewModel<StandingsRowViewModel>? standings)) return standings;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            standings = await client.GetFromJsonAsync<SeasonGroupedViewModel<StandingsRowViewModel>>($"liga/{leagueSlug}/tabla{query}");
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

        return null;
    }

    public async Task<SeasonGroupedViewModel<MatchdayGroupViewModel>?> GetResultsAsync(string leagueSlug, string? seasonSlug, string? divisionSlug, int? round)
    {
        string querySeason = string.IsNullOrWhiteSpace(seasonSlug) ? "" : $"season={seasonSlug}";
        string queryDiv = string.IsNullOrWhiteSpace(divisionSlug) || divisionSlug == "all" ? "" : $"division={divisionSlug}";
        string queryRound = round.HasValue ? $"round={round}" : "";
        string query = string.Join("&", new[] { querySeason, queryDiv, queryRound }.Where(q => !string.IsNullOrEmpty(q)));
        query = string.IsNullOrEmpty(query) ? "" : "?" + query;

        string cacheKey = $"resultados_{leagueSlug}_{seasonSlug ?? "default"}_{divisionSlug ?? "all"}_{round?.ToString() ?? "all"}";
        if (_cache.TryGetValue(cacheKey, out SeasonGroupedViewModel<MatchdayGroupViewModel>? results)) return results;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            results = await client.GetFromJsonAsync<SeasonGroupedViewModel<MatchdayGroupViewModel>>($"liga/{leagueSlug}/resultados{query}");
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

        return null;
    }

    public async Task<SeasonGroupedViewModel<MatchdayGroupViewModel>?> GetFixtureAsync(string leagueSlug, string? seasonSlug, string? divisionSlug, int? round)
    {
        string querySeason = string.IsNullOrWhiteSpace(seasonSlug) ? "" : $"season={seasonSlug}";
        string queryDiv = string.IsNullOrWhiteSpace(divisionSlug) || divisionSlug == "all" ? "" : $"division={divisionSlug}";
        string queryRound = round.HasValue ? $"round={round}" : "";
        string query = string.Join("&", new[] { querySeason, queryDiv, queryRound }.Where(q => !string.IsNullOrEmpty(q)));
        query = string.IsNullOrEmpty(query) ? "" : "?" + query;

        string cacheKey = $"fixture_{leagueSlug}_{seasonSlug ?? "default"}_{divisionSlug ?? "all"}_{round?.ToString() ?? "all"}";
        if (_cache.TryGetValue(cacheKey, out SeasonGroupedViewModel<MatchdayGroupViewModel>? fixture)) return fixture;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            fixture = await client.GetFromJsonAsync<SeasonGroupedViewModel<MatchdayGroupViewModel>>($"liga/{leagueSlug}/partidos{query}");
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

        return null;
    }
}
