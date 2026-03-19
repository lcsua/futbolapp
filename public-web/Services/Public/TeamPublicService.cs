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
            var result = await _httpClient.GetFromJsonAsync<TeamViewModel>($"{ApiBaseUrl}/teams/{slug}");
            if (result != null) return result;
        }
        catch { }

        return new TeamViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Equipo " + slug,
            Slug = slug,
            ShortName = slug.Substring(0, Math.Min(3, slug.Length)).ToUpper(),
            FoundedYear = 2010
        };
    }
}
