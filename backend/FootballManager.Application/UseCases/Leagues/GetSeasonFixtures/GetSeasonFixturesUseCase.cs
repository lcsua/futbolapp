using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;

public sealed class GetSeasonFixturesUseCase : IGetSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;

    public GetSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
    }

    public async Task<GetSeasonFixturesResponse?> ExecuteAsync(GetSeasonFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            return null;
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var draft = _draftStore.Get(request.SeasonId);
        if (draft != null)
            return new GetSeasonFixturesResponse(draft, isDraft: true);

        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        if (fixtures.Count == 0)
            return new GetSeasonFixturesResponse(new FixtureDraftDto(Array.Empty<FixtureDraftRoundDto>()), isDraft: false);

        var rounds = fixtures
            .GroupBy(f => new { f.RoundNumber, f.MatchDate })
            .OrderBy(g => g.Key.RoundNumber)
            .ThenBy(g => g.Key.MatchDate ?? DateOnly.MinValue)
            .Select(g => new FixtureDraftRoundDto(
                g.Key.RoundNumber,
                g.Key.MatchDate,
                g.OrderBy(f => f.StartTime ?? TimeOnly.MaxValue).ThenBy(f => f.Field?.Name ?? "")
                    .Select(f => new FixtureDraftMatchDto(
                        f.DivisionSeasonId,
                        f.DivisionSeason.Division.Name,
                        f.HomeTeamDivisionSeasonId,
                        f.HomeTeamDivisionSeason.Team.Name,
                        f.AwayTeamDivisionSeasonId,
                        f.AwayTeamDivisionSeason.Team.Name,
                        f.FieldId,
                        f.Field?.Name,
                        f.MatchDate,
                        f.StartTime
                    )).ToList()))
            .ToList();

        return new GetSeasonFixturesResponse(new FixtureDraftDto(rounds), isDraft: false);
    }
}
