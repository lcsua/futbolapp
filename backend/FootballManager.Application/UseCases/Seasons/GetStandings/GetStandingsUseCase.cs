using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Standings;
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
        var results = new List<CountedResult>();
        foreach (var fixture in fixtures)
        {
            if (fixture.Status != MatchStatus.COMPLETED || fixture.Result == null)
                continue;

            var home = ToSide(fixture.HomeTeamDivisionSeason, fixture.DivisionSeason);
            var away = ToSide(fixture.AwayTeamDivisionSeason, fixture.DivisionSeason);
            if (home == null || away == null)
                continue;

            results.Add(new CountedResult(
                home,
                away,
                fixture.Result.HomeTeamGoals,
                fixture.Result.AwayTeamGoals));
        }

        // Same ranking the simulator projects from. Sanctions are not stored, so none are applied.
        var projection = StandingsProjector.FromResults(results);
        var divisions = projection.Divisions
            .Select(division => new DivisionStandingsDto(
                division.DivisionId,
                division.DivisionName,
                division.Teams
                    .Select(team => new TeamStandingDto(
                        team.Position,
                        team.TeamId,
                        team.TeamName,
                        team.Points,
                        team.Played,
                        team.Wins,
                        team.Draws,
                        team.Losses,
                        team.GoalsFor,
                        team.GoalsAgainst,
                        team.GoalDifference))
                    .ToList()))
            .ToList();

        return new GetStandingsResponse(divisions);
    }

    /// <summary>Each side counts in its own zone, so an interzonal result lands in both tables.</summary>
    private static StandingSide? ToSide(TeamDivisionSeason? side, DivisionSeason? fixtureDivision)
    {
        if (side?.Team == null)
            return null;

        var divisionSeason = side.DivisionSeason ?? fixtureDivision;
        if (divisionSeason?.Division == null)
            return null;

        return new StandingSide(
            side.TeamId,
            side.Team.CompetitionName,
            divisionSeason.DivisionId,
            divisionSeason.Division.Name);
    }
}
