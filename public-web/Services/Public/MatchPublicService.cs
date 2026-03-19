using PublicWeb.Models.Public;
using System.Net.Http.Json;

namespace PublicWeb.Services.Public;

public class MatchPublicService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "http://localhost:5000/api";

    public MatchPublicService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MatchViewModel?> GetMatchByIdAsync(Guid id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<MatchViewModel>($"{ApiBaseUrl}/matches/{id}");
        }
        catch
        {
            return null;
        }
    }
}
