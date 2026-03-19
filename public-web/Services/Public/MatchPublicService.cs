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
            var result = await _httpClient.GetFromJsonAsync<MatchViewModel>($"{ApiBaseUrl}/matches/{id}");
            if (result != null) return result;
        }
        catch { }

        return new MatchViewModel
        {
            Id = id,
            Kickoff = DateTime.Now.AddHours(5),
            Status = "Scheduled",
            HomeTeam = new TeamViewModel { Name = "Local", Slug = "local" },
            AwayTeam = new TeamViewModel { Name = "Visitante", Slug = "visitante" }
        };
    }
}
