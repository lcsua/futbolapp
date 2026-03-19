using PublicWeb.Models.Public;
using System.Net.Http.Json;

namespace PublicWeb.Services.Public;

public class LeaguePublicService
{
    private readonly HttpClient _httpClient;
    private const string ApiBaseUrl = "http://localhost:5000/api";

    public LeaguePublicService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<LeagueViewModel?> GetLeagueBySlugAsync(string slug)
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<LeagueViewModel>($"{ApiBaseUrl}/leagues/{slug}");
            if (result != null) return result;
        }
        catch { }

        // Fallback simulado para que puedas ver el diseño de la web
        return new LeagueViewModel
        {
            Id = Guid.NewGuid(),
            Name = "Liga de Prueba Muestra",
            Slug = slug,
            Country = "Argentina",
            Description = "Esta es una liga de prueba para visualizar el diseño frontend."
        };
    }

    public async Task<List<StandingsRowViewModel>> GetStandingsAsync(string leagueSlug)
    {
        try
        {
            var standings = await _httpClient.GetFromJsonAsync<List<StandingsRowViewModel>>($"{ApiBaseUrl}/leagues/{leagueSlug}/standings");
            if (standings != null && standings.Any()) return standings;
        }
        catch { }

        // Fallback simulado
        return new List<StandingsRowViewModel>
        {
            new StandingsRowViewModel { Position = 1, Team = new TeamViewModel { Name = "Equipo A", Slug = "equipo-a" }, Played = 10, Won = 8, Drawn = 1, Lost = 1, GoalsFor = 20, GoalsAgainst = 5, Points = 25 },
            new StandingsRowViewModel { Position = 2, Team = new TeamViewModel { Name = "Equipo B", Slug = "equipo-b" }, Played = 10, Won = 7, Drawn = 2, Lost = 1, GoalsFor = 15, GoalsAgainst = 8, Points = 23 },
            new StandingsRowViewModel { Position = 3, Team = new TeamViewModel { Name = "Equipo C", Slug = "equipo-c" }, Played = 10, Won = 5, Drawn = 3, Lost = 2, GoalsFor = 12, GoalsAgainst = 9, Points = 18 },
        };
    }

    public async Task<List<MatchViewModel>> GetResultsAsync(string leagueSlug)
    {
        try
        {
            var results = await _httpClient.GetFromJsonAsync<List<MatchViewModel>>($"{ApiBaseUrl}/leagues/{leagueSlug}/results");
            if (results != null && results.Any()) return results;
        }
        catch { }

        return new List<MatchViewModel>
        {
            new MatchViewModel { Id = Guid.NewGuid(), Kickoff = DateTime.Now.AddDays(-2), Status = "Finished", HomeTeam = new TeamViewModel { Name="Equipo A", Slug="equipo-a" }, AwayTeam = new TeamViewModel { Name="Equipo B", Slug="equipo-b" }, HomeScore = 2, AwayScore = 1 }
        };
    }

    public async Task<List<MatchViewModel>> GetFixtureAsync(string leagueSlug)
    {
        try
        {
            var fixture = await _httpClient.GetFromJsonAsync<List<MatchViewModel>>($"{ApiBaseUrl}/leagues/{leagueSlug}/fixture");
            if (fixture != null && fixture.Any()) return fixture;
        }
        catch { }

        return new List<MatchViewModel>
        {
            new MatchViewModel { Id = Guid.NewGuid(), Kickoff = DateTime.Now.AddDays(2), Status = "Scheduled", HomeTeam = new TeamViewModel { Name="Equipo C", Slug="equipo-c" }, AwayTeam = new TeamViewModel { Name="Equipo D", Slug="equipo-d" } }
        };
    }
}
