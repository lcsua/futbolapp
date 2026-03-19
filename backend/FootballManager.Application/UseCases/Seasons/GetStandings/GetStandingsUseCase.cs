using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
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

        var byDivision = completed
            .GroupBy(f => (f.DivisionSeason.DivisionId, f.DivisionSeason.Division.Name))
            .ToList();

        var result = new List<DivisionStandingsDto>();
        foreach (var divGroup in byDivision.OrderBy(g => g.Key.Name))
        {
            var teamStats = new Dictionary<Guid, (Guid TeamId, string TeamName, int Played, int W, int D, int L, int GF, int GA)>();

            foreach (var f in divGroup)
            {
                var homeTeamId = f.HomeTeamDivisionSeason.TeamId;
                var homeTeamName = f.HomeTeamDivisionSeason.Team.Name;
                var awayTeamId = f.AwayTeamDivisionSeason.TeamId;
                var awayTeamName = f.AwayTeamDivisionSeason.Team.Name;
                var homeGoals = f.Result!.HomeTeamGoals;
                var awayGoals = f.Result!.AwayTeamGoals;

                EnsureTeam(teamStats, homeTeamId, homeTeamName);
                EnsureTeam(teamStats, awayTeamId, awayTeamName);

                var (_, _h, hp, hw, hd, hl, hgf, hga) = teamStats[homeTeamId];
                var (_, _a, ap, aw, ad, al, agf, aga) = teamStats[awayTeamId];

                hp += 1;
                ap += 1;
                hgf += homeGoals;
                hga += awayGoals;
                agf += awayGoals;
                aga += homeGoals;

                if (homeGoals > awayGoals)
                {
                    hw += 1;
                    al += 1;
                }
                else if (homeGoals < awayGoals)
                {
                    aw += 1;
                    hl += 1;
                }
                else
                {
                    hd += 1;
                    ad += 1;
                }

                teamStats[homeTeamId] = (homeTeamId, homeTeamName, hp, hw, hd, hl, hgf, hga);
                teamStats[awayTeamId] = (awayTeamId, awayTeamName, ap, aw, ad, al, agf, aga);
            }

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

            result.Add(new DivisionStandingsDto(divGroup.Key.DivisionId, divGroup.Key.Name, standings));
        }

        return new GetStandingsResponse(result);
    }

    private static void EnsureTeam(
        Dictionary<Guid, (Guid TeamId, string TeamName, int Played, int W, int D, int L, int GF, int GA)> teamStats,
        Guid teamId,
        string teamName)
    {
        if (!teamStats.ContainsKey(teamId))
            teamStats[teamId] = (teamId, teamName, 0, 0, 0, 0, 0, 0);
    }
}
