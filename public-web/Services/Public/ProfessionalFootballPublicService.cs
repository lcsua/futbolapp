using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using PublicWeb.Models.Public;

namespace PublicWeb.Services.Public;

public class ProfessionalFootballPublicService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ProfessionalFootballPublicService> _logger;

    public ProfessionalFootballPublicService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<ProfessionalFootballPublicService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(List<ProfessionalCompetitionCardViewModel> Items, bool Failed)> GetArgentineCompetitionsAsync()
    {
        const string cacheKey = "pro_ar_competitions";
        if (_cache.TryGetValue(cacheKey, out List<ProfessionalCompetitionCardViewModel>? cached) && cached != null)
            return (cached, false);

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var uri = new Uri(client.BaseAddress!, "professional-football/competitions");
            var dto = await client.GetFromJsonAsync<List<CompetitionSummaryDto>>(uri, JsonOptions);
            var items = (dto ?? new()).Select(MapCard).ToList();
            if (items.Count > 0)
                _cache.Set(cacheKey, items, TimeSpan.FromMinutes(10));
            return (items, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching professional competitions");
            return (new(), true);
        }
    }

    public async Task<ProfessionalCompetitionDetailViewModel?> GetCompetitionDetailAsync(string slug)
    {
        string cacheKey = $"pro_ar_detail_{slug}";
        if (_cache.TryGetValue(cacheKey, out ProfessionalCompetitionDetailViewModel? cached) && cached != null)
            return cached;

        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var uri = new Uri(client.BaseAddress!, $"professional-football/competitions/{slug}");
            using var response = await client.GetAsync(uri);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;
            if (!response.IsSuccessStatusCode)
            {
                return new ProfessionalCompetitionDetailViewModel
                {
                    LoadFailed = true,
                    ErrorMessage = "No pudimos obtener la información del torneo en este momento. Intentá nuevamente más tarde.",
                    Competition = new ProfessionalCompetitionCardViewModel { Slug = slug, Name = "Liga Profesional Argentina" }
                };
            }

            var dto = await response.Content.ReadFromJsonAsync<CompetitionDetailDto>(JsonOptions);
            if (dto?.Competition == null)
                return null;

            var model = new ProfessionalCompetitionDetailViewModel
            {
                Competition = MapCard(dto.Competition),
                Standings = (dto.Standings ?? new()).Select(g => new ProfessionalStandingGroupViewModel
                {
                    Name = g.Name,
                    Entries = (g.Entries ?? new()).Select(e => new ProfessionalStandingEntryViewModel
                    {
                        Position = e.Position,
                        TeamExternalId = e.TeamExternalId,
                        TeamName = e.TeamName,
                        TeamLogo = e.TeamLogo,
                        Played = e.Played,
                        Won = e.Won,
                        Drawn = e.Drawn,
                        Lost = e.Lost,
                        GoalsFor = e.GoalsFor,
                        GoalsAgainst = e.GoalsAgainst,
                        GoalDifference = e.GoalDifference,
                        Points = e.Points,
                    }).ToList(),
                }).ToList(),
                UpcomingMatches = (dto.UpcomingMatches ?? new()).Select(m => new ProfessionalMatchViewModel
                {
                    ExternalId = m.ExternalId,
                    Date = m.Date,
                    Status = m.Status,
                    StatusDetail = m.StatusDetail,
                    HomeTeamName = m.HomeTeam?.Name ?? "",
                    HomeTeamLogo = m.HomeTeam?.LogoUrl,
                    AwayTeamName = m.AwayTeam?.Name ?? "",
                    AwayTeamLogo = m.AwayTeam?.LogoUrl,
                    Venue = m.Venue,
                    HomeScore = m.HomeScore,
                    AwayScore = m.AwayScore,
                }).ToList(),
            };

            _cache.Set(cacheKey, model, TimeSpan.FromMinutes(5));
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching professional competition {Slug}", slug);
            return new ProfessionalCompetitionDetailViewModel
            {
                LoadFailed = true,
                ErrorMessage = "No pudimos obtener la información del torneo en este momento. Intentá nuevamente más tarde.",
                Competition = new ProfessionalCompetitionCardViewModel { Slug = slug, Name = "Liga Profesional Argentina" }
            };
        }
    }

    private static ProfessionalCompetitionCardViewModel MapCard(CompetitionSummaryDto dto) =>
        new()
        {
            Slug = dto.Slug,
            Name = dto.Name,
            Country = dto.Country,
            LogoUrl = dto.LogoUrl,
            Season = dto.Season,
            CurrentTournament = dto.CurrentTournament,
        };

    private sealed class CompetitionSummaryDto
    {
        public string Slug { get; set; } = "";
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string? LogoUrl { get; set; }
        public int Season { get; set; }
        public string CurrentTournament { get; set; } = "";
    }

    private sealed class CompetitionDetailDto
    {
        public CompetitionSummaryDto? Competition { get; set; }
        public List<StandingGroupDto>? Standings { get; set; }
        public List<MatchDto>? UpcomingMatches { get; set; }
    }

    private sealed class StandingGroupDto
    {
        public string Name { get; set; } = "";
        public List<StandingEntryDto>? Entries { get; set; }
    }

    private sealed class StandingEntryDto
    {
        public int Position { get; set; }
        public string TeamExternalId { get; set; } = "";
        public string TeamName { get; set; } = "";
        public string? TeamLogo { get; set; }
        public int Played { get; set; }
        public int Won { get; set; }
        public int Drawn { get; set; }
        public int Lost { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference { get; set; }
        public int Points { get; set; }
    }

    private sealed class MatchDto
    {
        public string ExternalId { get; set; } = "";
        public DateTimeOffset Date { get; set; }
        public string Status { get; set; } = "";
        public string StatusDetail { get; set; } = "";
        public TeamDto? HomeTeam { get; set; }
        public TeamDto? AwayTeam { get; set; }
        public string? Venue { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
    }

    private sealed class TeamDto
    {
        public string ExternalId { get; set; } = "";
        public string Name { get; set; } = "";
        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }
    }
}
