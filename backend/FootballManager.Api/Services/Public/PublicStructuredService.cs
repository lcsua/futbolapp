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
            .Where(s => s.LeagueId == leagueId)
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
        var teams = await _db.Teams
            .Where(t => t.LeagueId == leagueId)
            .ToListAsync(cancellationToken);

        var targetSlug = SlugHelper.NormalizeSlug(teamSlug);
        return teams.FirstOrDefault(t => SlugHelper.NormalizeSlug(t.Name) == targetSlug);
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

    public async Task<TeamSummaryPublicDto?> GetTeamSummaryAsync(string leagueSlug, string seasonSlug, string teamSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await ResolveSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var team = await ResolveTeamAsync(league.Id, teamSlug, cancellationToken);
        if (team == null) return null;

        var response = new TeamSummaryPublicDto
        {
            Team = new TeamPublicDto
            {
                Id = team.Id,
                Name = team.Name,
                Slug = teamSlug,
                ShortName = team.Name.Substring(0, Math.Min(team.Name.Length, 3)).ToUpper(),
                LogoUrl = team.LogoUrl
            }
        };

        response.ActiveSeasons.Add(new SeasonPublicDto { Id = season.Id, Name = season.Name });

        var fixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
            .Where(f => f.DivisionSeason!.SeasonId == season.Id && 
                       (f.HomeTeamDivisionSeason.TeamId == team.Id || f.AwayTeamDivisionSeason.TeamId == team.Id))
            .OrderBy(f => f.MatchDate).ThenBy(f => f.StartTime)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var upcoming = fixtures.Where(f => f.Status != Domain.Enums.MatchStatus.COMPLETED && (f.MatchDate == null || f.MatchDate >= DateOnly.FromDateTime(now))).Take(5).ToList();
        var recent = fixtures.Where(f => f.Status == Domain.Enums.MatchStatus.COMPLETED).OrderByDescending(f => f.MatchDate).Take(5).ToList();

        response.NextMatches = upcoming.Select(f => MapToMatchDto(f)).ToList();
        response.LastResults = recent.Select(f => MapToMatchDto(f)).ToList();

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
                Team = new TeamPublicDto { Id = r.TeamId, Name = r.TeamName, Slug = SlugHelper.NormalizeSlug(r.TeamName) }
            }).ToList();
        }

        return summary;
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

        return fixtures.Select(MapToMatchDto).ToList();
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

        return fixtures.Select(MapToMatchDto).ToList();
    }

    private MatchPublicDto MapToMatchDto(Fixture match)
    {
        return new MatchPublicDto
        {
            Id = match.Id,
            Status = match.Status.ToString(),
            HomeScore = match.Result?.HomeTeamGoals,
            AwayScore = match.Result?.AwayTeamGoals,
            HomeTeam = new TeamPublicDto 
            { 
                Id = match.HomeTeamDivisionSeason?.TeamId ?? Guid.Empty, 
                Name = match.HomeTeamDivisionSeason?.Team?.Name ?? "Local", 
                Slug = SlugHelper.NormalizeSlug(match.HomeTeamDivisionSeason?.Team?.Name) 
            },
            AwayTeam = new TeamPublicDto 
            { 
                Id = match.AwayTeamDivisionSeason?.TeamId ?? Guid.Empty, 
                Name = match.AwayTeamDivisionSeason?.Team?.Name ?? "Visitante", 
                Slug = SlugHelper.NormalizeSlug(match.AwayTeamDivisionSeason?.Team?.Name) 
            },
            Kickoff = DateTime.TryParse(match.MatchDate?.ToString("yyyy-MM-dd") + " " + match.StartTime?.ToString("HH:mm"), out var dt) ? dt : DateTime.UtcNow
        };
    }

    public async Task<List<SeasonPublicDto>> GetLeagueMetaAsync(string leagueSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return new();

        var seasons = await _db.Set<Season>()
            .Where(s => s.LeagueId == league.Id)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.EndDate)
            .ToListAsync(cancellationToken);

        return seasons.Select(s => new SeasonPublicDto
        {
            Id = s.Id,
            Name = s.Name,
            Slug = SlugHelper.NormalizeSlug(s.Name),
            EndDate = s.EndDate,
            IsActive = s.IsActive
        }).ToList();
    }

    private async Task<Season?> GetTargetSeasonAsync(Guid leagueId, string? seasonSlug, CancellationToken cancellationToken)
    {
        var seasons = await _db.Set<Season>()
            .Where(s => s.LeagueId == leagueId)
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

    public async Task<SeasonGroupedDto<StandingsRowPublicDto>?> GetLeagueStandingsAsync(string leagueSlug, string? seasonSlug, CancellationToken cancellationToken = default)
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

        var divSeasons = await _db.Set<DivisionSeason>()
            .Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id)
            .ToListAsync(cancellationToken);

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
                    Team = new TeamPublicDto { Id = r.TeamId, Name = r.TeamName, Slug = SlugHelper.NormalizeSlug(r.TeamName) }
                }).ToList();
            }

            result.Divisions.Add(group);
        }

        return result;
    }

    public async Task<SeasonGroupedDto<MatchPublicDto>?> GetLeagueResultsAsync(string leagueSlug, string? seasonSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var result = new SeasonGroupedDto<MatchPublicDto> { SeasonName = season.Name, SeasonSlug = SlugHelper.NormalizeSlug(season.Name) };

        var divSeasons = await _db.Set<DivisionSeason>().Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id).ToListAsync(cancellationToken);

        var allFixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Where(f => f.SeasonId == season.Id && f.Status == Domain.Enums.MatchStatus.COMPLETED)
            .OrderByDescending(f => f.MatchDate).ThenByDescending(f => f.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var ds in divSeasons.OrderBy(x => x.Division.Name))
        {
            var matches = allFixtures.Where(f => f.DivisionSeasonId == ds.Id).Select(MapToMatchDto).ToList();
            if (matches.Any())
            {
                result.Divisions.Add(new DivisionGroupDto<MatchPublicDto>
                {
                    DivisionName = ds.Division.Name,
                    DivisionSlug = SlugHelper.NormalizeSlug(ds.Division.Name),
                    Data = matches
                });
            }
        }
        return result;
    }

    public async Task<SeasonGroupedDto<MatchPublicDto>?> GetLeagueMatchesAsync(string leagueSlug, string? seasonSlug, CancellationToken cancellationToken = default)
    {
        var league = await GetLeagueIfPublicAsync(leagueSlug, cancellationToken);
        if (league == null) return null;

        var season = await GetTargetSeasonAsync(league.Id, seasonSlug, cancellationToken);
        if (season == null) return null;

        var result = new SeasonGroupedDto<MatchPublicDto> { SeasonName = season.Name, SeasonSlug = SlugHelper.NormalizeSlug(season.Name) };

        var divSeasons = await _db.Set<DivisionSeason>().Include(ds => ds.Division)
            .Where(ds => ds.SeasonId == season.Id).ToListAsync(cancellationToken);

        var allFixtures = await _db.Set<Fixture>()
            .Include(f => f.HomeTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.AwayTeamDivisionSeason).ThenInclude(td => td.Team)
            .Include(f => f.Result)
            .Where(f => f.SeasonId == season.Id && f.Status != Domain.Enums.MatchStatus.COMPLETED)
            .OrderBy(f => f.MatchDate).ThenBy(f => f.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var ds in divSeasons.OrderBy(x => x.Division.Name))
        {
            var matches = allFixtures.Where(f => f.DivisionSeasonId == ds.Id).Select(MapToMatchDto).ToList();
            if (matches.Any())
            {
                result.Divisions.Add(new DivisionGroupDto<MatchPublicDto>
                {
                    DivisionName = ds.Division.Name,
                    DivisionSlug = SlugHelper.NormalizeSlug(ds.Division.Name),
                    Data = matches
                });
            }
        }
        return result;
    }
}
