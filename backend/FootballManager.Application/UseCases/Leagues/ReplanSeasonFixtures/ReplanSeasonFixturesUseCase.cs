using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.ReplanSeasonFixtures;

/// <summary>
/// Builds a draft that re-plans the pending matches of one or more zones from a given matchday,
/// keeping everything already played. See <see cref="FixtureReplanScheduler"/>.
/// </summary>
public sealed class ReplanSeasonFixturesUseCase : IReplanSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly ICompetitionRuleRepository _competitionRuleRepository;
    private readonly IRoundSlotAssignerFactory _slotAssignerFactory;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;

    public ReplanSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        ICompetitionRuleRepository competitionRuleRepository,
        IRoundSlotAssignerFactory slotAssignerFactory,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _competitionRuleRepository = competitionRuleRepository ?? throw new ArgumentNullException(nameof(competitionRuleRepository));
        _slotAssignerFactory = slotAssignerFactory ?? throw new ArgumentNullException(nameof(slotAssignerFactory));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
    }

    public async Task<FixtureDraftDto> ExecuteAsync(ReplanSeasonFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        if (request.FromRound < 1)
            throw new BusinessException("La fecha desde la que se replanifica debe ser 1 o mayor.");

        var divisionIds = request.DivisionIds.Distinct().ToList();
        if (divisionIds.Count == 0)
            throw new BusinessException("Elegí al menos una zona para replanificar.");

        var existingDraft = _draftStore.Get(request.SeasonId);
        if (existingDraft != null && existingDraft.Replan == null)
            throw new BusinessException("Hay un borrador de fixture sin guardar. Guardalo o descartalo antes de replanificar.");

        var competitionRule = await _competitionRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, null, cancellationToken)
            ?? throw new BusinessException("Competition rules must be configured at league level before generating fixtures.");
        var matchDays = competitionRule.MatchDays.Select(m => m.DayOfWeek).Distinct().OrderBy(d => d).ToList();

        var seasonDivisions = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var selected = seasonDivisions.Where(ds => divisionIds.Contains(ds.DivisionId)).ToList();
        if (selected.Count != divisionIds.Count)
            throw new BusinessException("Alguna de las zonas elegidas no está asignada a esta temporada.");

        var divisionSeasonById = seasonDivisions.ToDictionary(ds => ds.Id);
        var tdsById = seasonDivisions.SelectMany(ds => ds.TeamAssignments).ToDictionary(t => t.Id);
        var selectedTeamIds = selected.SelectMany(ds => ds.TeamAssignments).Select(t => t.Id).ToHashSet();

        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);

        bool IsLocked(Fixture f) =>
            f.RoundNumber < request.FromRound || f.Status != MatchStatus.SCHEDULED || f.Result != null;

        var involved = fixtures
            .Where(f => selectedTeamIds.Contains(f.HomeTeamDivisionSeasonId) || selectedTeamIds.Contains(f.AwayTeamDivisionSeasonId))
            .ToList();
        var removable = involved
            .Where(f => !IsLocked(f)
                        && selectedTeamIds.Contains(f.HomeTeamDivisionSeasonId)
                        && selectedTeamIds.Contains(f.AwayTeamDivisionSeasonId))
            .ToList();
        var removableIds = removable.Select(f => f.Id).ToHashSet();

        var existing = involved
            .Where(f => !removableIds.Contains(f.Id))
            .Select(f => new FixtureReplanScheduler.ExistingMatch(
                f.HomeTeamDivisionSeasonId,
                f.AwayTeamDivisionSeasonId,
                f.RoundNumber - request.FromRound))
            .ToList();

        var zones = selected
            .Select(ds => new FixtureReplanScheduler.Zone(ds.Id, ds.TeamAssignments.Select(t => t.Id).ToList()))
            .ToList();

        var plan = FixtureReplanScheduler.Plan(zones, existing, competitionRule.IsHomeAway, request.FillByesWithInterzonal);
        if (plan.All(r => r.Count == 0))
            throw new BusinessException("No quedan partidos por programar en las zonas elegidas.");

        var slotAssigner = await _slotAssignerFactory.CreateAsync(request.LeagueId, seasonDivisions, cancellationToken);

        var keptFixtures = fixtures.Where(f => !removableIds.Contains(f.Id)).ToList();
        var knownRoundDates = fixtures
            .Where(f => f.MatchDate.HasValue)
            .GroupBy(f => f.RoundNumber)
            .ToDictionary(g => g.Key, g => g.Min(f => f.MatchDate!.Value));

        var warnings = new List<string>();
        var newMatchesByRound = new Dictionary<int, List<FixtureDraftMatchDto>>();
        DateOnly? previousDate = null;

        for (var i = 0; i < plan.Count; i++)
        {
            var roundNumber = request.FromRound + i;
            var matchDate = ResolveRoundDate(roundNumber, previousDate, knownRoundDates, matchDays, season.StartDate);
            previousDate = matchDate;

            var roundMatches = new List<(DivisionSeason Ds, TeamDivisionSeason Home, TeamDivisionSeason Away, bool Interzonal)>();
            foreach (var m in plan[i])
            {
                var home = tdsById[m.HomeTeamId];
                var away = tdsById[m.AwayTeamId];
                roundMatches.Add((divisionSeasonById[home.DivisionSeasonId], home, away, m.IsInterzonal));
            }

            var slots = await slotAssigner.AssignAsync(
                matchDate,
                roundMatches.Select(x => (x.Ds, x.Home, x.Away)).ToList(),
                keptFixtures);
            if (roundMatches.Count > 0 && slots == null)
                warnings.Add($"Fecha {roundNumber} ({matchDate:dd/MM}): no alcanzaron las canchas/horarios; los partidos quedaron sin cancha ni hora.");

            var draftMatches = new List<FixtureDraftMatchDto>();
            for (var m = 0; m < roundMatches.Count; m++)
            {
                var (ds, home, away, interzonal) = roundMatches[m];
                Guid? fieldId = slots?[m].FieldId;
                TimeOnly? kickoff = slots?[m].Start;

                draftMatches.Add(new FixtureDraftMatchDto(
                    ds.Id,
                    ds.Division.Name,
                    home.Id,
                    home.Team.CompetitionName,
                    away.Id,
                    away.Team.CompetitionName,
                    fieldId,
                    slotAssigner.FieldName(fieldId),
                    matchDate,
                    kickoff,
                    IsInterzonal: interzonal,
                    AwayDivisionName: interzonal ? divisionSeasonById[away.DivisionSeasonId].Division.Name : null,
                    IsReplanned: true));
            }

            newMatchesByRound[roundNumber] = draftMatches;
        }

        var rounds = BuildRounds(keptFixtures, newMatchesByRound, seasonDivisions);
        var draft = new FixtureDraftDto(
            rounds,
            new FixtureReplanDto(request.FromRound, selected.Select(ds => ds.Id).ToList(), removableIds.ToList(), warnings));

        _draftStore.Set(request.SeasonId, draft);
        return draft;
    }

    private static List<FixtureDraftRoundDto> BuildRounds(
        IReadOnlyList<Fixture> keptFixtures,
        IReadOnlyDictionary<int, List<FixtureDraftMatchDto>> newMatchesByRound,
        IReadOnlyList<DivisionSeason> seasonDivisions)
    {
        var matchesByRound = keptFixtures
            .GroupBy(f => f.RoundNumber)
            .ToDictionary(g => g.Key, g => g.Select(FixtureDraftMapper.ToDraftMatch).ToList());

        foreach (var (round, matches) in newMatchesByRound)
        {
            if (!matchesByRound.TryGetValue(round, out var list))
                matchesByRound[round] = list = new List<FixtureDraftMatchDto>();
            list.AddRange(matches);
        }

        return matchesByRound
            .OrderBy(x => x.Key)
            .Select(x =>
            {
                var matches = x.Value
                    .OrderBy(m => m.Date ?? DateOnly.MaxValue)
                    .ThenBy(m => m.KickoffTime ?? TimeOnly.MaxValue)
                    .ThenBy(m => m.FieldName ?? string.Empty)
                    .ToList();
                var playing = matches
                    .SelectMany(m => new[] { m.HomeTeamDivisionSeasonId, m.AwayTeamDivisionSeasonId })
                    .ToHashSet();
                var byes = seasonDivisions
                    .Where(ds => ds.TeamAssignments.Any(t => playing.Contains(t.Id)))
                    .SelectMany(ds => ds.TeamAssignments
                        .Where(t => !playing.Contains(t.Id))
                        .Select(t => new FixtureDraftByeDto(ds.Id, ds.Division.Name, t.Id, t.Team.CompetitionName)))
                    .OrderBy(b => b.DivisionName)
                    .ThenBy(b => b.TeamName)
                    .ToList();
                var roundDate = matches.Select(m => m.Date).Where(d => d.HasValue).DefaultIfEmpty(null).Min();
                return new FixtureDraftRoundDto(x.Key, roundDate, matches, byes);
            })
            .ToList();
    }

    private static DateOnly ResolveRoundDate(
        int roundNumber,
        DateOnly? previousDate,
        IReadOnlyDictionary<int, DateOnly> knownRoundDates,
        IReadOnlyList<int> matchDays,
        DateOnly seasonStart)
    {
        if (knownRoundDates.TryGetValue(roundNumber, out var known) && (previousDate == null || known > previousDate))
            return known;
        if (previousDate.HasValue)
            return NextMatchDay(previousDate.Value, matchDays);

        var lastKnown = knownRoundDates.Where(k => k.Key < roundNumber).OrderByDescending(k => k.Key).Select(k => (int?)k.Key).FirstOrDefault();
        if (lastKnown is int lastRound)
        {
            var date = knownRoundDates[lastRound];
            for (var r = lastRound; r < roundNumber; r++)
                date = NextMatchDay(date, matchDays);
            return date;
        }

        var cursor = seasonStart.AddDays(-1);
        for (var r = 0; r < roundNumber; r++)
            cursor = NextMatchDay(cursor, matchDays);
        return cursor;
    }

    private static DateOnly NextMatchDay(DateOnly after, IReadOnlyList<int> matchDays)
    {
        if (matchDays.Count == 0)
            return after.AddDays(7);
        for (var i = 1; i <= 7; i++)
        {
            var candidate = after.AddDays(i);
            if (matchDays.Contains((int)candidate.DayOfWeek))
                return candidate;
        }
        return after.AddDays(7);
    }
}
