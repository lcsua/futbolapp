using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Models.Public;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Matches.GetMatches;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

public class PublicStructuredService
{
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IDivisionRepository _divisionRepository;
    private readonly IGetStandingsUseCase _getStandingsUseCase;
    private readonly IGetMatchesUseCase _getMatchesUseCase;

    public PublicStructuredService(
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        ITeamRepository teamRepository,
        IDivisionRepository divisionRepository,
        IGetStandingsUseCase getStandingsUseCase,
        IGetMatchesUseCase getMatchesUseCase)
    {
        _leagueRepository = leagueRepository;
        _seasonRepository = seasonRepository;
        _teamRepository = teamRepository;
        _divisionRepository = divisionRepository;
        _getStandingsUseCase = getStandingsUseCase;
        _getMatchesUseCase = getMatchesUseCase;
    }

    public async Task<TeamSummaryPublicDto?> GetTeamSummaryAsync(string leagueSlug, string season, string teamSlug, CancellationToken cancellationToken = default)
    {
        var league = await _leagueRepository.GetBySlugAsync(leagueSlug, cancellationToken);
        if (league == null || !league.IsPublic) return null;

        var seasonEntity = await _seasonRepository.GetByLeagueIdAndNameAsync(league.Id, season, cancellationToken);
        if (seasonEntity == null) return null;

        var team = await _teamRepository.GetByLeagueIdAndSlugAsync(league.Id, teamSlug, cancellationToken);
        if (team == null) return null;

        var allSeasons = await _seasonRepository.GetByLeagueIdAsync(league.Id, cancellationToken);
        var activeSeasons = allSeasons
            .Where(s => s.EndDate == null || s.EndDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SeasonPublicDto { Id = s.Id, Name = s.Name })
            .ToList();

        var standingsReq = new GetStandingsRequest { LeagueId = league.Id, SeasonId = seasonEntity.Id, IsPublic = true, UserId = Guid.Empty };
        var standingsRes = await _getStandingsUseCase.ExecuteAsync(standingsReq, cancellationToken);

        var matchesReq = new GetMatchesRequest { LeagueId = league.Id, SeasonId = seasonEntity.Id, IsPublic = true, UserId = Guid.Empty };
        var matchesRes = await _getMatchesUseCase.ExecuteAsync(matchesReq, cancellationToken);

        var allMatches = matchesRes.Rounds.SelectMany(r => r.Matches).ToList();
        var results = allMatches
            .Where(m => (m.Status == "COMPLETED" || m.Status == "Finished") && (m.HomeTeamId == team.Id || m.AwayTeamId == team.Id))
            .OrderByDescending(m => m.MatchDate + " " + m.KickoffTime)
            .Take(10)
            .Select(m => new MatchPublicDto
            {
                Id = m.Id,
                Status = m.Status,
                HomeScore = m.HomeScore,
                AwayScore = m.AwayScore,
                HomeTeam = new TeamPublicDto { Id = m.HomeTeamId, Name = m.HomeTeamName ?? "", Slug = m.HomeTeamId.ToString() },
                AwayTeam = new TeamPublicDto { Id = m.AwayTeamId, Name = m.AwayTeamName ?? "", Slug = m.AwayTeamId.ToString() },
                Kickoff = DateTime.TryParse(m.MatchDate + " " + m.KickoffTime, out var dt) ? dt : DateTime.UtcNow
            })
            .ToList();

        var nextMatches = allMatches
            .Where(m => m.Status != "COMPLETED" && m.Status != "Finished" && (m.HomeTeamId == team.Id || m.AwayTeamId == team.Id))
            .OrderBy(m => m.MatchDate + " " + m.KickoffTime)
            .Take(5)
            .Select(m => new MatchPublicDto
            {
                Id = m.Id,
                Status = m.Status,
                HomeTeam = new TeamPublicDto { Id = m.HomeTeamId, Name = m.HomeTeamName ?? "", Slug = m.HomeTeamId.ToString() },
                AwayTeam = new TeamPublicDto { Id = m.AwayTeamId, Name = m.AwayTeamName ?? "", Slug = m.AwayTeamId.ToString() },
                Kickoff = DateTime.TryParse(m.MatchDate + " " + m.KickoffTime, out var dt) ? dt : DateTime.UtcNow
            })
            .ToList();

        var teamStanding = standingsRes.Divisions
            .SelectMany(d => d.Standings.Where(s => s.TeamId == team.Id))
            .FirstOrDefault();

        return new TeamSummaryPublicDto
        {
            Team = new TeamPublicDto { Id = team.Id, Name = team.Name, Slug = team.Slug, ShortName = team.ShortName, LogoUrl = team.LogoUrl },
            ActiveSeasons = activeSeasons,
            NextMatches = nextMatches,
            LastResults = results,
            Standing = teamStanding != null ? new StandingSummaryDto
            {
                Position = teamStanding.Position,
                Played = teamStanding.Played,
                Points = teamStanding.Points,
                Wins = teamStanding.Wins,
                Draws = teamStanding.Draws,
                Losses = teamStanding.Losses
            } : null
        };
    }

    public async Task<DivisionSummaryPublicDto?> GetDivisionSummaryAsync(string leagueSlug, string season, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await _leagueRepository.GetBySlugAsync(leagueSlug, cancellationToken);
        if (league == null || !league.IsPublic) return null;

        var seasonEntity = await _seasonRepository.GetByLeagueIdAndNameAsync(league.Id, season, cancellationToken);
        if (seasonEntity == null) return null;

        var division = await _divisionRepository.GetByLeagueIdAndSlugAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return null;

        var req = new GetStandingsRequest { LeagueId = league.Id, SeasonId = seasonEntity.Id, IsPublic = true, UserId = Guid.Empty };
        var res = await _getStandingsUseCase.ExecuteAsync(req, cancellationToken);

        var divStandings = res.Divisions.FirstOrDefault(d => d.DivisionId == division.Id);
        if (divStandings == null) return null;

        var standings = divStandings.Standings.Select(s => new StandingsRowPublicDto
        {
            Position = s.Position,
            Played = s.Played,
            Won = s.Wins,
            Drawn = s.Draws,
            Lost = s.Losses,
            GoalsFor = s.GoalsFor,
            GoalsAgainst = s.GoalsAgainst,
            Points = s.Points,
            Team = new TeamPublicDto { Id = s.TeamId, Name = s.TeamName, Slug = s.TeamId.ToString() }
        }).ToList();

        return new DivisionSummaryPublicDto
        {
            Division = new DivisionPublicDto { Id = division.Id, Name = division.Name, Slug = division.Slug },
            Standings = standings
        };
    }

    public async Task<List<MatchPublicDto>?> GetDivisionResultsAsync(string leagueSlug, string season, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await _leagueRepository.GetBySlugAsync(leagueSlug, cancellationToken);
        if (league == null || !league.IsPublic) return null;

        var seasonEntity = await _seasonRepository.GetByLeagueIdAndNameAsync(league.Id, season, cancellationToken);
        if (seasonEntity == null) return null;

        var division = await _divisionRepository.GetByLeagueIdAndSlugAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return null;

        var req = new GetMatchesRequest { LeagueId = league.Id, SeasonId = seasonEntity.Id, DivisionId = division.Id, IsPublic = true, UserId = Guid.Empty };
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
                    HomeTeam = new TeamPublicDto { Id = match.HomeTeamId, Name = match.HomeTeamName ?? "", Slug = match.HomeTeamId.ToString() },
                    AwayTeam = new TeamPublicDto { Id = match.AwayTeamId, Name = match.AwayTeamName ?? "", Slug = match.AwayTeamId.ToString() },
                    Kickoff = DateTime.TryParse(match.MatchDate + " " + match.KickoffTime, out var dt) ? dt : DateTime.UtcNow
                });
            }
        }
        return results.OrderByDescending(m => m.Kickoff).ToList();
    }

    public async Task<List<MatchPublicDto>?> GetDivisionMatchesAsync(string leagueSlug, string season, string divisionSlug, CancellationToken cancellationToken = default)
    {
        var league = await _leagueRepository.GetBySlugAsync(leagueSlug, cancellationToken);
        if (league == null || !league.IsPublic) return null;

        var seasonEntity = await _seasonRepository.GetByLeagueIdAndNameAsync(league.Id, season, cancellationToken);
        if (seasonEntity == null) return null;

        var division = await _divisionRepository.GetByLeagueIdAndSlugAsync(league.Id, divisionSlug, cancellationToken);
        if (division == null) return null;

        var req = new GetMatchesRequest { LeagueId = league.Id, SeasonId = seasonEntity.Id, DivisionId = division.Id, IsPublic = true, UserId = Guid.Empty };
        var res = await _getMatchesUseCase.ExecuteAsync(req, cancellationToken);

        var matches = new List<MatchPublicDto>();
        foreach (var group in res.Rounds)
        {
            foreach (var match in group.Matches.Where(m => m.Status != "COMPLETED" && m.Status != "Finished"))
            {
                matches.Add(new MatchPublicDto
                {
                    Id = match.Id,
                    Status = match.Status,
                    HomeTeam = new TeamPublicDto { Id = match.HomeTeamId, Name = match.HomeTeamName ?? "", Slug = match.HomeTeamId.ToString() },
                    AwayTeam = new TeamPublicDto { Id = match.AwayTeamId, Name = match.AwayTeamName ?? "", Slug = match.AwayTeamId.ToString() },
                    Kickoff = DateTime.TryParse(match.MatchDate + " " + match.KickoffTime, out var dt) ? dt : DateTime.UtcNow
                });
            }
        }
        return matches.OrderBy(m => m.Kickoff).ToList();
    }
}
