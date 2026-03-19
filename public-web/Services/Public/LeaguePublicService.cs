using PublicWeb.Models.Public;
using System.Net.Http.Json;

namespace PublicWeb.Services.Public;

public class LeaguePublicService
{
    private readonly HttpClient _httpClient;
    // Base URL of the existing API. In a real scenario, this would come from appsettings.json.
    private const string ApiBaseUrl = "http://localhost:5000/api";

    public LeaguePublicService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LeagueViewModel?> GetLeagueBySlugAsync(string slug)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<LeagueViewModel>($"{ApiBaseUrl}/leagues/{slug}");
        }
        catch (HttpRequestException)
        {
            // Fallback for missing backend or 404
            return null;
        }
    }

    public async Task<List<StandingsRowViewModel>> GetStandingsAsync(string leagueSlug)
    {
        try
        {
            var standings = await _httpClient.GetFromJsonAsync<List<StandingsRowViewModel>>($"{ApiBaseUrl}/standings/{leagueSlug}");
            return standings ?? new List<StandingsRowViewModel>();
        }
        catch
        {
            return new List<StandingsRowViewModel>();
        }
    }

    public async Task<List<MatchViewModel>> GetResultsAsync(string leagueSlug)
    {
        try
        {
            var results = await _httpClient.GetFromJsonAsync<List<MatchViewModel>>($"{ApiBaseUrl}/leagues/{leagueSlug}/results");
            return results ?? new List<MatchViewModel>();
        }
        catch
        {
            return new List<MatchViewModel>();
        }
    }

    public async Task<List<MatchViewModel>> GetFixtureAsync(string leagueSlug)
    {
        try
        {
            var fixture = await _httpClient.GetFromJsonAsync<List<MatchViewModel>>($"{ApiBaseUrl}/leagues/{leagueSlug}/fixture");
            return fixture ?? new List<MatchViewModel>();
        }
        catch
        {
            return new List<MatchViewModel>();
        }
    }
}
