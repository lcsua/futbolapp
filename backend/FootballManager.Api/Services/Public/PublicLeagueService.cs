using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Models.Public;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.GetLeague;
using FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;
using FootballManager.Application.UseCases.Matches.GetMatches;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

public class PublicLeagueService
{
    private readonly IGetLeagueUseCase _getLeagueUseCase;
    private readonly IGetStandingsUseCase _getStandingsUseCase;
    private readonly IGetMatchesUseCase _getMatchesUseCase;
    private readonly IGetSeasonFixturesUseCase _getSeasonFixturesUseCase;
    private readonly ILeagueRepository _leagueRepository;

    public PublicLeagueService(
        IGetLeagueUseCase getLeagueUseCase,
        IGetStandingsUseCase getStandingsUseCase,
        IGetMatchesUseCase getMatchesUseCase,
        IGetSeasonFixturesUseCase getSeasonFixturesUseCase,
        ILeagueRepository leagueRepository)
    {
        _getLeagueUseCase = getLeagueUseCase;
        _getStandingsUseCase = getStandingsUseCase;
        _getMatchesUseCase = getMatchesUseCase;
        _getSeasonFixturesUseCase = getSeasonFixturesUseCase;
        _leagueRepository = leagueRepository;
    }

    private Guid ParseSlug(string slug)
    {
        if (Guid.TryParse(slug, out var id)) return id;
        throw new KeyNotFoundException("Invalid slug format.");
    }

    private async Task<Guid?> GetActiveSeasonIdAsync(Guid leagueId, CancellationToken cancellationToken)
    {
        var league = await _leagueRepository.GetByIdAsync(leagueId, cancellationToken);
        if (league == null) return null;
        var activeSeason = league.Seasons.OrderByDescending(s => s.StartDate).FirstOrDefault();
        return activeSeason?.Id;
    }

    public async Task<LeaguePublicDto?> GetLeagueAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var leagueId = ParseSlug(slug);
            var req = new GetLeagueRequest(leagueId, Guid.Empty, isPublic: true);
            var res = await _getLeagueUseCase.ExecuteAsync(req, cancellationToken);
            
            return new LeaguePublicDto
            {
                Id = res.League.Id,
                Name = res.League.Name,
                Slug = res.League.Id.ToString(), // Or slug parameter
                Country = res.League.Country,
                Description = res.League.Description
            };
        }
        catch (KeyNotFoundException) { return null; }
    }

    public async Task<List<StandingsRowPublicDto>> GetStandingsAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var leagueId = ParseSlug(slug);
            var seasonId = await GetActiveSeasonIdAsync(leagueId, cancellationToken);
            if (!seasonId.HasValue) return new List<StandingsRowPublicDto>();

            var req = new GetStandingsRequest { LeagueId = leagueId, SeasonId = seasonId.Value, IsPublic = true, UserId = Guid.Empty };
            var res = await _getStandingsUseCase.ExecuteAsync(req, cancellationToken);

            var standingsList = new List<StandingsRowPublicDto>();
            if (res.Divisions == null || !res.Divisions.Any()) return standingsList;
            
            // For public API, we simplify by returning the first division or combining them if needed. (Normally leagues have one main division).
            var firstDiv = res.Divisions.First();
            foreach (var r in firstDiv.Standings)
            {
                standingsList.Add(new StandingsRowPublicDto
                {
                    Position = r.Position,
                    Played = r.Played,
                    Won = r.Wins,
                    Drawn = r.Draws,
                    Lost = r.Losses,
                    GoalsFor = r.GoalsFor,
                    GoalsAgainst = r.GoalsAgainst,
                    Points = r.Points,
                    Team = new TeamPublicDto { Id = r.TeamId, Name = r.TeamName, Slug = r.TeamId.ToString() }
                });
            }

            return standingsList;
        }
        catch { return new List<StandingsRowPublicDto>(); }
    }

    public async Task<List<MatchPublicDto>> GetResultsAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var leagueId = ParseSlug(slug);
            var seasonId = await GetActiveSeasonIdAsync(leagueId, cancellationToken);
            if (!seasonId.HasValue) return new List<MatchPublicDto>();

            var req = new GetMatchesRequest { LeagueId = leagueId, SeasonId = seasonId.Value, IsPublic = true, UserId = Guid.Empty };
            var res = await _getMatchesUseCase.ExecuteAsync(req, cancellationToken);

            var results = new List<MatchPublicDto>();
            foreach (var group in res.Rounds)
            {
                foreach (var match in group.Matches.Where(m => m.Status == "COMPLETED" || m.Status == "Finished"))
                {
                    results.Add(new MatchPublicDto
                    {
                        Id = match.Id,
                        Status = match.Status,
                        HomeScore = match.HomeScore,
                        AwayScore = match.AwayScore,
                        HomeTeam = new TeamPublicDto { Id = match.HomeTeamId, Name = match.HomeTeamName ?? "Local", Slug = match.HomeTeamId.ToString() },
                        AwayTeam = new TeamPublicDto { Id = match.AwayTeamId, Name = match.AwayTeamName ?? "Visitante", Slug = match.AwayTeamId.ToString() },
                        Kickoff = DateTime.TryParse(match.MatchDate + " " + match.KickoffTime, out var dt) ? dt : DateTime.UtcNow // Approximate Kickoff parse
                    });
                }
            }

            // Return latest 20 results
            return results.OrderByDescending(m => m.Kickoff).Take(20).ToList();
        }
        catch { return new List<MatchPublicDto>(); }
    }

    public async Task<List<MatchPublicDto>> GetFixtureAsync(string slug, CancellationToken cancellationToken = default)
    {
        try
        {
            var leagueId = ParseSlug(slug);
            var seasonId = await GetActiveSeasonIdAsync(leagueId, cancellationToken);
            if (!seasonId.HasValue) return new List<MatchPublicDto>();

            var req = new GetMatchesRequest { LeagueId = leagueId, SeasonId = seasonId.Value, IsPublic = true, UserId = Guid.Empty };
            var res = await _getMatchesUseCase.ExecuteAsync(req, cancellationToken);

            var fixtures = new List<MatchPublicDto>();
            foreach (var group in res.Rounds)
            {
                foreach (var match in group.Matches.Where(m => m.Status != "COMPLETED" && m.Status != "Finished"))
                {
                    fixtures.Add(new MatchPublicDto
                    {
                        Id = match.Id,
                        Status = match.Status,
                        HomeTeam = new TeamPublicDto { Id = match.HomeTeamId, Name = match.HomeTeamName ?? "Local", Slug = match.HomeTeamId.ToString() },
                        AwayTeam = new TeamPublicDto { Id = match.AwayTeamId, Name = match.AwayTeamName ?? "Visitante", Slug = match.AwayTeamId.ToString() },
                        Kickoff = DateTime.TryParse(match.MatchDate + " " + match.KickoffTime, out var dt) ? dt : DateTime.UtcNow
                    });
                }
            }

            return fixtures.OrderBy(m => m.Kickoff).Take(20).ToList();
        }
        catch { return new List<MatchPublicDto>(); }
    }
}
