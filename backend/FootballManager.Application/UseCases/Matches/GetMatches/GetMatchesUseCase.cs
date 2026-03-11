using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Matches.GetMatches;

public sealed class GetMatchesUseCase : IGetMatchesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;

    public GetMatchesUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IDivisionSeasonRepository divisionSeasonRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
    }

    public async Task<GetMatchesResponse> ExecuteAsync(GetMatchesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        Guid? divisionSeasonId = null;
        if (request.DivisionId.HasValue)
        {
            var ds = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(request.SeasonId, request.DivisionId.Value, cancellationToken);
            if (ds == null)
                return new GetMatchesResponse(new List<MatchRoundGroupDto>());
            divisionSeasonId = ds.Id;
        }

        var fixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
            request.SeasonId, divisionSeasonId, request.Round, cancellationToken);

        var groups = fixtures
            .GroupBy(f => (f.RoundNumber, f.DivisionSeason.Division.Name))
            .OrderBy(g => g.Key.RoundNumber)
            .ThenBy(g => g.Key.Name)
            .Select(g => new MatchRoundGroupDto(
                g.Key.RoundNumber,
                g.Key.Name,
                g.Select(f => ToMatchListItem(f)).ToList()))
            .ToList();

        return new GetMatchesResponse(groups);
    }

    private static MatchListItemDto ToMatchListItem(Fixture f)
    {
        var homeTeam = f.HomeTeamDivisionSeason?.Team;
        var awayTeam = f.AwayTeamDivisionSeason?.Team;
        var homeScore = f.Result?.HomeTeamGoals;
        var awayScore = f.Result?.AwayTeamGoals;
        return new MatchListItemDto(
            f.Id,
            f.DivisionSeasonId,
            f.DivisionSeason?.Division?.Name ?? "",
            f.RoundNumber,
            homeTeam?.Name ?? "",
            homeTeam?.Id ?? Guid.Empty,
            awayTeam?.Name ?? "",
            awayTeam?.Id ?? Guid.Empty,
            homeScore,
            awayScore,
            f.Status.ToString(),
            f.StartTime?.ToString("HH:mm") ?? "",
            f.MatchDate?.ToString("yyyy-MM-dd") ?? "",
            f.Field?.Name ?? "",
            homeTeam?.LogoUrl,
            awayTeam?.LogoUrl);
    }
}
