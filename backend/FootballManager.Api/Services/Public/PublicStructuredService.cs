using FootballManager.Api.Helpers;
using FootballManager.Api.Models.Public;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using FootballManager.Domain.Entities;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

public class PublicStructuredService
{
    private readonly FootballManagerDbContext _db;
    private readonly IGetStandingsUseCase _getStandingsUseCase;

    public PublicStructuredService(FootballManagerDbContext db, IGetStandingsUseCase getStandingsUseCase)
    {
        _db = db;
        _getStandingsUseCase = getStandingsUseCase;
    }

    private async Task<League?> GetLeagueIfPublicAsync(string leagueSlug, CancellationToken cancellationToken)
    {
        return await _db.Leagues
            .FirstOrDefaultAsync(l => l.Slug == leagueSlug && l.IsPublic, cancellationToken);
    }

    private async Task<Season?> ResolveSeasonAsync(Guid leagueId, string seasonSlug, CancellationToken cancellationToken)
    {
        var seasons = await _db.Set<Season>()
            .Where(s => s.LeagueId == leagueId && s.IsPublic)
            .ToListAsync(cancellationToken);

        var targetSlug = SlugHelper.NormalizeSlug(seasonSlug);
        return seasons.FirstOrDefault(s => SlugHelper.NormalizeSlug(s.Name) == targetSlug);
    }

    private async Task<Division?> ResolveDivisionAsync(Guid leagueId, string divisionSlug, CancellationToken cancellationToken)
    {
        var divisions = await _db.Set<Division>()
            .Where(d => d.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        var targetSlug = SlugHelper.NormalizeSlug(divisionSlug);
        return divisions.FirstOrDefault(d => SlugHelper.NormalizeSlug(d.Name) == targetSlug);
    }

    private async Task<Team?> ResolveTeamAsync(Guid leagueId, string teamSlug, CancellationToken cancellationToken)
    {
        var targetSlug = SoftNormalizeTeamSlug(teamSlug);
        if (string.IsNullOrWhiteSpace(targetSlug)) return null;

        var bySlug = await _db.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.LeagueId == leagueId && t.Slug == targetSlug, cancellationToken);
        if (bySlug != null) return bySlug;

        // Fallback for older links that used NormalizeSlug(Name) instead of persisted Team.Slug
        var teams = await _db.Teams
            .AsNoTracking()
            .Where(t => t.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        return teams.FirstOrDefault(t =>
            SoftNormalizeTeamSlug(t.Slug) == targetSlug ||
            SoftNormalizeTeamSlug(t.Name) == targetSlug ||
            SoftNormalizeTeamSlug(t.DisplayName) == targetSlug);
    }

    private static string SoftNormalizeTeamSlug(string? value) => SlugHelper.NormalizeSlug(value);

    private static TeamPublicDto MapTeamDto(Team? team, string? leagueSlug = null)
    {
        if (team == null)
        {
            return new TeamPublicDto();
        }

        return new TeamPublicDto
        {
            Id = team.Id,
            Name = team.DisplayName,
            Slug = string.IsNullOrWhiteSpace(team.Slug) ? SoftNormalizeTeamSlug(team.DisplayName) : team.Slug,
            ShortName = string.IsNullOrWhiteSpace(team.ShortName)
                ? team.DisplayName.Substring(0, Math.Min(team.DisplayName.Length, 3)).ToUpperInvariant()
                : team.ShortName,
            LogoUrl = string.IsNullOrWhiteSpace(team.LogoUrl) ? null : team.LogoUrl
        };
    }

    private async Task<Dictionary<Guid, Team>> GetLeagueTeamsMapAsync(Guid leagueId, CancellationToken cancellationToken)
    {
        return await _db.Teams
            .AsNoTracking()
            .Where(t => t.LeagueId == leagueId)
            .ToDictionaryAsync(t => t.Id, cancellationToken);
    }

    public async Task<List<LeaguePublicDto>> GetPublicLeaguesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Leagues
            .AsNoTracking()
            .Where(l => l.IsPublic && l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LeaguePublicDto
            {
                Id = l.Id,
                Name = l.Name,
                Slug = l.Slug,
                Country = l.Country ?? string.Empty,
                Description = l.Description ?? string.Empty
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaguePublicDto?> GetLeagueSummaryAsync(string leagueSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        return new LeaguePublicDto
        {
            Id = league.Id,
            Name = league.Name,
            Slug = league.Slug,
            Country = league.Country ?? string.Empty,
            Description = league.Description ?? string.Empty
        };
    }

    public async Task<TeamSummaryPublicDto?> GetTeamSummaryAsync(
        string leagueSlug,
        string teamSlug,
        string? seasonSlug = null,
        int nextPage = 1,
        int resultsPage = 1,
        int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var team = await ResolveTeamAsync(league.Id, teamSlug, cancellationToken);
        if (team == null) return null;

        pageSize = Math.Clamp(pageSize, 1, 20);
        nextPage = Math.Max(1, nextPage);
        resultsPage = Math.Max(1, resultsPage);

        var response = new TeamSummaryPublicDto
        {
            Team = MapTeamDto(team),
            League = new LeaguePublicDto
            {
                Id = league.Id,
                Name = league.Name,
                Slug = league.Slug,
                Country = league.Country ?? string.Empty,
                Description = league.Description ?? string.Empty
            },
            Season = new SeasonPublicDto
            {
                Id = season.Id,
                Name = season.Name,
                Slug = SlugHelper.NormalizeSlug(season.Name),
                EndDate = season.EndDate,
                IsActive = season.IsActive
            },
            PageSize = pageSize,
            NextMatchesPage = nextPage,
            LastResultsPage = resultsPage
        };

        response.ActiveSeasons.Add(response.Season);

        var fixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
            .Where(f => f.DivisionSeason!.SeasonId == season.Id &&
                       (f.HomeTeamDivisionSeason.TeamId == team.Id || f.AwayTeamDivisionSeason.TeamId == team.Id))
            .OrderBy(f => f.MatchDate).ThenBy(f => f.StartTime)
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcomingAll = fixtures
            .Where(f => f.Status != Domain.Enums.MatchStatus.COMPLETED &&
                        (f.MatchDate == null || f.MatchDate >= today))
            .ToList();
        var recentAll = fixtures
            .Where(f => f.Status == Domain.Enums.MatchStatus.COMPLETED)
            .OrderByDescending(f => f.MatchDate)
            .ThenByDescending(f => f.StartTime)
            .ToList();

        response.NextMatchesTotal = upcomingAll.Count;
        response.LastResultsTotal = recentAll.Count;

        var nextPages = Math.Max(1, (int)Math.Ceiling(response.NextMatchesTotal / (double)pageSize));
        var resultsPages = Math.Max(1, (int)Math.Ceiling(response.LastResultsTotal / (double)pageSize));
        response.NextMatchesPage = Math.Min(nextPage, nextPages);
        response.LastResultsPage = Math.Min(resultsPage, resultsPages);

        var upcoming = upcomingAll
            .Skip((response.NextMatchesPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        var recent = recentAll
            .Skip((response.LastResultsPage - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Skip heavy crest payloads on list rows (team header already has LogoUrl).
        response.NextMatches = upcoming.Select(f => MapToMatchDto(f, league.Slug, includeLogos: false)).ToList();
        response.LastResults = recent.Select(f => MapToMatchDto(f, league.Slug, includeLogos: false)).ToList();

        var standingsReq = new GetStandingsRequest { LeagueId = league.Id, SeasonId = season.Id, IsPublic = true };
        var standingsRes = await _getStandingsUseCase.ExecuteAsync(standingsReq, cancellationToken);
        foreach (var division in standingsRes.Divisions)
        {
            var row = division.Standings.FirstOrDefault(s => s.TeamId == team.Id);
            if (row == null) continue;

            response.Standing = new StandingSummaryDto
            {
                Position = row.Position,
                Played = row.Played,
                Points = row.Points,
                Wins = row.Wins,
                Draws = row.Draws,
                Losses = row.Losses,
                GoalsFor = row.GoalsFor,
                GoalsAgainst = row.GoalsAgainst,
                DivisionName = division.DivisionName
            };
            break;
        }

        return response;
    }

    public async Task<DivisionSummaryPublicDto?> GetDivisionSummaryAsync(string leagueSlug, string seasonSlug, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await ResolveSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var division = await ResolveDivisionAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return null;

        var summary = new DivisionSummaryPublicDto
        {
            Division = new DivisionPublicDto
            {
                Id = division.Id,
                Name = division.Name,
                Slug = divisionSlug
            }
        };

        var teamsMap = await GetLeagueTeamsMapAsync(league.Id, cancellationToken);
        var standingsReq = new GetStandingsRequest { LeagueId = league.Id, SeasonId = season.Id, IsPublic = true };
        var standingsRes = await _getStandingsUseCase.ExecuteAsync(standingsReq, cancellationToken);
        
        var divisionStandings = standingsRes.Divisions.FirstOrDefault(d => d.DivisionId == division.Id);
        if (divisionStandings != null)
        {
            summary.Standings = divisionStandings.Standings.Select(r => new StandingsRowPublicDto
            {
                Position = r.Position,
                Played = r.Played,
                Won = r.Wins,
                Drawn = r.Draws,
                Lost = r.Losses,
                GoalsFor = r.GoalsFor,
                GoalsAgainst = r.GoalsAgainst,
                Points = r.Points,
                Team = MapStandingTeam(r.TeamId, r.TeamName, teamsMap)
            }).ToList();
        }

        return summary;
    }


    private static TeamPublicDto MapStandingTeam(Guid teamId, string teamName, Dictionary<Guid, Team> teamsMap)
    {
        if (teamsMap.TryGetValue(teamId, out var team))
            return MapTeamDto(team);

        return new TeamPublicDto
        {
            Id = teamId,
            Name = teamName,
            Slug = SoftNormalizeTeamSlug(teamName)
        };
    }
    public async Task<List<MatchPublicDto>> GetDivisionResultsAsync(string leagueSlug, string seasonSlug, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return new List<MatchPublicDto>();

        var season = await ResolveSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return new List<MatchPublicDto>();

        var division = await ResolveDivisionAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return new List<MatchPublicDto>();

        var divSeason = await _db.Set<DivisionSeason>().FirstOrDefaultAsync(ds => ds.SeasonId == season.Id && ds.DivisionId == division.Id, cancellationToken);
        if (divSeason == null) return new List<MatchPublicDto>();

        var fixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Where(f => f.DivisionSeasonId == divSeason.Id && f.Status == Domain.Enums.MatchStatus.COMPLETED)
            .OrderByDescending(f => f.MatchDate).ThenByDescending(f => f.StartTime)
            .Take(50)
            .ToListAsync(cancellationToken);

        return fixtures.Select(f => MapToMatchDto(f, league.Slug)).ToList();
    }

    public async Task<List<MatchPublicDto>> GetDivisionMatchesAsync(string leagueSlug, string seasonSlug, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return new List<MatchPublicDto>();

        var season = await ResolveSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return new List<MatchPublicDto>();

        var division = await ResolveDivisionAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return new List<MatchPublicDto>();

        var divSeason = await _db.Set<DivisionSeason>().FirstOrDefaultAsync(ds => ds.SeasonId == season.Id && ds.DivisionId == division.Id, cancellationToken);
        if (divSeason == null) return new List<MatchPublicDto>();

        var fixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Where(f => f.DivisionSeasonId == divSeason.Id && f.Status != Domain.Enums.MatchStatus.COMPLETED)
            .OrderBy(f => f.MatchDate).ThenBy(f => f.StartTime)
            .Take(50)
            .ToListAsync(cancellationToken);

        return fixtures.Select(f => MapToMatchDto(f, league.Slug)).ToList();
    }

    private MatchPublicDto MapToMatchDto(Fixture match, string? leagueSlug = null, bool includeLogos = true)
    {
        var homeTeam = match.HomeTeamDivisionSeason?.Team;
        var awayTeam = match.AwayTeamDivisionSeason?.Team;

        var home = homeTeam != null
            ? MapTeamDto(homeTeam)
            : new TeamPublicDto { Name = "Local", Slug = "local" };
        var away = awayTeam != null
            ? MapTeamDto(awayTeam)
            : new TeamPublicDto { Name = "Visitante", Slug = "visitante" };

        if (!includeLogos)
        {
            home.LogoUrl = null;
            away.LogoUrl = null;
        }

        return new MatchPublicDto
        {
            Id = match.Id,
            Status = match.Status.ToString(),
            HomeScore = match.Result?.HomeTeamGoals,
            AwayScore = match.Result?.AwayTeamGoals,
            LeagueSlug = leagueSlug,
            HomeTeam = home,
            AwayTeam = away,
            Kickoff = DateTime.TryParse(match.MatchDate?.ToString("yyyy-MM-dd") + " " + match.StartTime?.ToString("HH:mm"), out var dt) ? dt : DateTime.UtcNow
        };
    }

    public async Task<List<SeasonPublicDto>> GetLeagueMetaAsync(string leagueSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return new();

        var seasons = await _db.Set<Season>()
            .Include(s => s.DivisionSeasons)
            .ThenInclude(ds => ds.Division)
            .Where(s => s.LeagueId == league.Id && s.IsPublic)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.EndDate)
            .ToListAsync(cancellationToken);

        return seasons.Select(s => new SeasonPublicDto
        {
            Id = s.Id,
            Name = s.Name,
            Slug = SlugHelper.NormalizeSlug(s.Name),
            EndDate = s.EndDate,
            IsActive = s.IsActive,
            Divisions = s.DivisionSeasons.Select(ds => new DivisionPublicDto
            {
                Id = ds.Division.Id,
                Name = ds.Division.Name,
                Slug = SlugHelper.NormalizeSlug(ds.Division.Name)
            }).OrderBy(d => d.Name).ToList()
        }).ToList();
    }

    private async Task<Season?> GetTargetSeasonAsync(Guid leagueId, string? seasonSlug, CancellationToken cancellationToken)
    {
        var seasons = await _db.Set<Season>()
            .Where(s => s.LeagueId == leagueId && s.IsPublic)
            .ToListAsync(cancellationToken);

        if (seasons.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(seasonSlug))
        {
            var targetSlug = SlugHelper.NormalizeSlug(seasonSlug);
            var season = seasons.FirstOrDefault(s => SlugHelper.NormalizeSlug(s.Name) == targetSlug);
            if (season != null) return season;
        }

        return seasons.OrderByDescending(s => s.IsActive).ThenByDescending(s => s.EndDate).First();
    }

    public async Task<SeasonGroupedDto<StandingsRowPublicDto>?> GetLeagueStandingsAsync(string leagueSlug, string? seasonSlug, string? divisionSlug = null, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var result = new SeasonGroupedDto<StandingsRowPublicDto>
        {
            SeasonName = season.Name,
            SeasonSlug = SlugHelper.NormalizeSlug(season.Name)
        };

        var divSeasonsQuery = _db.Set<DivisionSeason>()
            .Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id);

        var divSeasons = await divSeasonsQuery.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(divisionSlug) && divisionSlug.ToLowerInvariant() != "all")
        {
            var targetDivSlug = SlugHelper.NormalizeSlug(divisionSlug);
            divSeasons = divSeasons.Where(ds => SlugHelper.NormalizeSlug(ds.Division.Name) == targetDivSlug).ToList();
        }

        var teamsMap = await GetLeagueTeamsMapAsync(league.Id, cancellationToken);
        var standingsReq = new GetStandingsRequest { LeagueId = league.Id, SeasonId = season.Id, IsPublic = true };
        var standingsRes = await _getStandingsUseCase.ExecuteAsync(standingsReq, cancellationToken);

        foreach (var ds in divSeasons.OrderBy(x => x.Division.Name))
        {
            var group = new DivisionGroupDto<StandingsRowPublicDto>
            {
                DivisionName = ds.Division.Name,
                DivisionSlug = SlugHelper.NormalizeSlug(ds.Division.Name)
            };

            var divisionStandings = standingsRes.Divisions.FirstOrDefault(d => d.DivisionId == ds.DivisionId);
            if (divisionStandings != null)
            {
                group.Data = divisionStandings.Standings.Select(r => new StandingsRowPublicDto
                {
                    Position = r.Position,
                    Played = r.Played,
                    Won = r.Wins,
                    Drawn = r.Draws,
                    Lost = r.Losses,
                    GoalsFor = r.GoalsFor,
                    GoalsAgainst = r.GoalsAgainst,
                    Points = r.Points,
                    Team = MapStandingTeam(r.TeamId, r.TeamName, teamsMap)
                }).ToList();
            }

            result.Divisions.Add(group);
        }

        return result;
    }

    public async Task<SeasonGroupedDto<MatchdayGroupDto>?> GetLeagueResultsAsync(string leagueSlug, string? seasonSlug, string? divisionSlug = null, int? round = null, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var result = new SeasonGroupedDto<MatchdayGroupDto> { SeasonName = season.Name, SeasonSlug = SlugHelper.NormalizeSlug(season.Name) };

        var divSeasons = await _db.Set<DivisionSeason>().Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id).ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(divisionSlug) && divisionSlug.ToLowerInvariant() != "all")
        {
            var targetDivSlug = SlugHelper.NormalizeSlug(divisionSlug);
            divSeasons = divSeasons.Where(ds => SlugHelper.NormalizeSlug(ds.Division.Name) == targetDivSlug).ToList();
        }

        var allFixturesQuery = _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Where(f => f.SeasonId == season.Id && f.Status == Domain.Enums.MatchStatus.COMPLETED);

        if (round.HasValue)
        {
            allFixturesQuery = allFixturesQuery.Where(f => f.RoundNumber == round.Value);
        }

        var allFixtures = await allFixturesQuery
            .OrderByDescending(f => f.MatchDate).ThenByDescending(f => f.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var ds in divSeasons.OrderBy(x => x.Division.Name))
        {
            var matchesForDiv = allFixtures.Where(f => f.DivisionSeasonId == ds.Id).Select(f => MapToMatchDto(f, league.Slug)).ToList();
            if (matchesForDiv.Any())
            {
                var matchdays = matchesForDiv.GroupBy(m => allFixtures.First(f => f.Id == m.Id).RoundNumber)
                    .Select(g => new MatchdayGroupDto { Round = g.Key, Matches = g.ToList() })
                    .OrderByDescending(md => md.Round)
                    .ToList();

                result.Divisions.Add(new DivisionGroupDto<MatchdayGroupDto>
                {
                    DivisionName = ds.Division.Name,
                    DivisionSlug = SlugHelper.NormalizeSlug(ds.Division.Name),
                    Data = matchdays
                });
            }
        }
        return result;
    }

    public async Task<SeasonGroupedDto<MatchdayGroupDto>?> GetLeagueMatchesAsync(string leagueSlug, string? seasonSlug, string? divisionSlug = null, int? round = null, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var result = new SeasonGroupedDto<MatchdayGroupDto> { SeasonName = season.Name, SeasonSlug = SlugHelper.NormalizeSlug(season.Name) };

        var divSeasons = await _db.Set<DivisionSeason>().Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id).ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(divisionSlug) && divisionSlug.ToLowerInvariant() != "all")
        {
            var targetDivSlug = SlugHelper.NormalizeSlug(divisionSlug);
            divSeasons = divSeasons.Where(ds => SlugHelper.NormalizeSlug(ds.Division.Name) == targetDivSlug).ToList();
        }

        var allFixturesQuery = _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Where(f => f.SeasonId == season.Id && f.Status != Domain.Enums.MatchStatus.COMPLETED);

        if (round.HasValue)
        {
            allFixturesQuery = allFixturesQuery.Where(f => f.RoundNumber == round.Value);
        }

        var allFixtures = await allFixturesQuery
            .OrderBy(f => f.MatchDate).ThenBy(f => f.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var ds in divSeasons.OrderBy(x => x.Division.Name))
        {
            var matchesForDiv = allFixtures.Where(f => f.DivisionSeasonId == ds.Id).Select(f => MapToMatchDto(f, league.Slug)).ToList();
            if (matchesForDiv.Any())
            {
                var matchdays = matchesForDiv.GroupBy(m => allFixtures.First(f => f.Id == m.Id).RoundNumber)
                    .Select(g => new MatchdayGroupDto { Round = g.Key, Matches = g.ToList() })
                    .OrderBy(md => md.Round)
                    .ToList();

                result.Divisions.Add(new DivisionGroupDto<MatchdayGroupDto>
                {
                    DivisionName = ds.Division.Name,
                    DivisionSlug = SlugHelper.NormalizeSlug(ds.Division.Name),
                    Data = matchdays
                });
            }
        }
        return result;
    }
}
