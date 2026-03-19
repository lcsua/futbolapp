using PublicWeb.Models.Public;
using System.Net.Http.Json;

namespace PublicWeb.Services.Public;

public class TeamPublicService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "http://localhost:5000/api";

    public TeamPublicService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TeamViewModel?> GetTeamBySlugAsync(string slug)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TeamViewModel>($"{ApiBaseUrl}/teams/{slug}");
        }
        catch
        {
            return null;
        }
    }
}
