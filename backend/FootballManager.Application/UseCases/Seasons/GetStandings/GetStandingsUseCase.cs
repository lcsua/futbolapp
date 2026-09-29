using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Seasons.GetStandings;

public sealed class GetStandingsUseCase : IGetStandingsUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;

    public GetStandingsUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
    }

    public async Task<GetStandingsResponse> ExecuteAsync(GetStandingsRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = request.IsPublic || await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var completed = fixtures
            .Where(f => f.Status == MatchStatus.COMPLETED && f.Result != null)
            .ToList();

        // Each side counts in its own zone, so interzonal results land in both zone tables.
        var byDivision = new Dictionary<(Guid DivisionId, string Name), Dictionary<Guid, (Guid TeamId, string TeamName, int Played, int W, int D, int L, int GF, int GA)>>();

        foreach (var f in completed)
        {
            var homeGoals = f.Result!.HomeTeamGoals;
            var awayGoals = f.Result!.AwayTeamGoals;
            AddSide(byDivision, f.HomeTeamDivisionSeason, f.DivisionSeason, homeGoals, awayGoals);
            AddSide(byDivision, f.AwayTeamDivisionSeason, f.DivisionSeason, awayGoals, homeGoals);
        }

        var result = new List<DivisionStandingsDto>();
        foreach (var (divisionKey, teamStats) in byDivision.OrderBy(g => g.Key.Name))
        {
            var standings = teamStats.Values
                .Select(t =>
                {
                    var pts = t.W * 3 + t.D;
                    var gd = t.GF - t.GA;
                    return (t.TeamId, t.TeamName, pts, t.Played, t.W, t.D, t.L, t.GF, t.GA, gd);
                })
                .OrderByDescending(x => x.pts)
                .ThenByDescending(x => x.gd)
                .ThenByDescending(x => x.GF)
                .Select((x, i) => new TeamStandingDto(i + 1, x.TeamId, x.TeamName, x.pts, x.Played, x.W, x.D, x.L, x.GF, x.GA, x.gd))
                .ToList();

            result.Add(new DivisionStandingsDto(divisionKey.DivisionId, divisionKey.Name, standings));
        }

        return new GetStandingsResponse(result);
    }

    private static void AddSide(
        Dictionary<(Guid DivisionId, string Name), Dictionary<Guid, (Guid TeamId, string TeamName, int Played, int W, int D, int L, int GF, int GA)>> byDivision,
        TeamDivisionSeason side,
        DivisionSeason fixtureDivisionSeason,
        int goalsFor,
        int goalsAgainst)
    {
        var teamDivisionSeason = side.DivisionSeason ?? fixtureDivisionSeason;
        var key = (teamDivisionSeason.DivisionId, teamDivisionSeason.Division.Name);
        if (!byDivision.TryGetValue(key, out var teamStats))
            byDivision[key] = teamStats = new Dictionary<Guid, (Guid, string, int, int, int, int, int, int)>();

        var (_, _, played, w, d, l, gf, ga) = teamStats.TryGetValue(side.TeamId, out var current)
            ? current
            : (side.TeamId, side.Team.CompetitionName, 0, 0, 0, 0, 0, 0);

        if (goalsFor > goalsAgainst) w++;
        else if (goalsFor < goalsAgainst) l++;
        else d++;

        teamStats[side.TeamId] = (side.TeamId, side.Team.CompetitionName, played + 1, w, d, l, gf + goalsFor, ga + goalsAgainst);
    }
}
