using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;

public sealed class GetSeasonFixturesUseCase : IGetSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureDraftStore _draftStore;

    public GetSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureRepository fixtureRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureDraftStore draftStore)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
    }

    public async Task<GetSeasonFixturesResponse?> ExecuteAsync(GetSeasonFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = request.IsPublic || await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
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

        var divisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var divisionsById = divisionSeasons.ToDictionary(ds => ds.Id);

        var rounds = fixtures
            .GroupBy(f => new { f.RoundNumber, f.MatchDate })
            .OrderBy(g => g.Key.RoundNumber)
            .ThenBy(g => g.Key.MatchDate ?? DateOnly.MinValue)
            .Select(g =>
            {
                var matches = g.OrderBy(f => f.StartTime ?? TimeOnly.MaxValue).ThenBy(f => f.Field?.Name ?? "")
                    .Select(FixtureDraftMapper.ToDraftMatch)
                    .ToList();

                var byes = InferByesForRound(g.ToList(), divisionsById);
                return new FixtureDraftRoundDto(g.Key.RoundNumber, g.Key.MatchDate, matches, byes);
            })
            .ToList();

        return new GetSeasonFixturesResponse(new FixtureDraftDto(rounds), isDraft: false);
    }

    private static List<FixtureDraftByeDto> InferByesForRound(
        List<Fixture> roundFixtures,
        Dictionary<Guid, DivisionSeason> divisionsById)
    {
        var byes = new List<FixtureDraftByeDto>();
        var playing = roundFixtures
            .SelectMany(f => new[] { f.HomeTeamDivisionSeasonId, f.AwayTeamDivisionSeasonId })
            .ToHashSet();

        foreach (var ds in divisionsById.Values)
        {
            // Only infer when this round has matches for the division (import/generate scope).
            if (!ds.TeamAssignments.Any(ta => playing.Contains(ta.Id))) continue;

            foreach (var ta in ds.TeamAssignments)
            {
                if (playing.Contains(ta.Id)) continue;
                byes.Add(new FixtureDraftByeDto(
                    ds.Id,
                    ds.Division.Name,
                    ta.Id,
                    ta.Team.CompetitionName));
            }
        }

        return byes;
    }
}
