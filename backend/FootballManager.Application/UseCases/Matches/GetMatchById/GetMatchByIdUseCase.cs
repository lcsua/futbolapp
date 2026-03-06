using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Matches.GetMatchById;

public sealed class GetMatchByIdUseCase : IGetMatchByIdUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;

    public GetMatchByIdUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
    }

    public async Task<GetMatchByIdResponse> ExecuteAsync(GetMatchByIdRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(request.MatchId, cancellationToken);
        if (fixture == null)
            throw new KeyNotFoundException($"Match {request.MatchId} not found.");
        if (fixture.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        var homeTeam = fixture.HomeTeamDivisionSeason?.Team;
        var awayTeam = fixture.AwayTeamDivisionSeason?.Team;
        var incidents = (fixture.Incidents ?? new List<MatchIncident>())
            .OrderBy(i => i.Minute)
            .Select(i => new MatchIncidentDto(
                i.Id,
                i.Minute,
                i.TeamId,
                i.Team?.Name,
                i.PlayerName,
                i.IncidentType.ToString(),
                i.Notes))
            .ToList();

        return new GetMatchByIdResponse(
            fixture.Id,
            fixture.RoundNumber,
            fixture.DivisionSeason?.Division?.Name ?? "",
            homeTeam?.Name ?? "",
            homeTeam?.Id ?? Guid.Empty,
            awayTeam?.Name ?? "",
            awayTeam?.Id ?? Guid.Empty,
            fixture.Result?.HomeTeamGoals,
            fixture.Result?.AwayTeamGoals,
            fixture.Status.ToString(),
            fixture.StartTime.ToString("HH:mm"),
            fixture.MatchDate.ToString("yyyy-MM-dd"),
            fixture.Field?.Name ?? "",
            incidents,
            homeTeam?.LogoUrl,
            awayTeam?.LogoUrl);
    }
}
