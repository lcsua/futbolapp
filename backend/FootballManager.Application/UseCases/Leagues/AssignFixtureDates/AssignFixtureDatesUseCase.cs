using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Push;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.AssignFixtureDates;

public sealed class AssignFixtureDatesUseCase : IAssignFixtureDatesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly ICompetitionRuleRepository _competitionRuleRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPushNotificationService _pushNotifications;
    private readonly ILeagueRepository _leagueRepository;

    public AssignFixtureDatesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        ICompetitionRuleRepository competitionRuleRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork,
        IPushNotificationService pushNotifications,
        ILeagueRepository leagueRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _competitionRuleRepository = competitionRuleRepository ?? throw new ArgumentNullException(nameof(competitionRuleRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _pushNotifications = pushNotifications ?? throw new ArgumentNullException(nameof(pushNotifications));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
    }

    public async Task<AssignFixtureDatesResponse> ExecuteAsync(
        AssignFixtureDatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");

        SeasonGuard.EnsureOpen(season);

        var competitionRule = await _competitionRuleRepository.GetByLeagueAndSeasonAsync(
            request.LeagueId, null, cancellationToken);
        if (competitionRule == null)
            return AssignFixtureDatesResponse.WithErrors(new[]
            {
                "Configurá las reglas de competición de la liga antes de asignar fechas."
            });

        var matchDays = competitionRule.MatchDays
            .OrderBy(m => m.DayOfWeek)
            .Select(m => m.DayOfWeek)
            .ToList();
        if (matchDays.Count == 0)
            return AssignFixtureDatesResponse.WithErrors(new[]
            {
                "Definí al menos un día de juego en las reglas de competición (ej. sábado)."
            });

        HashSet<Guid>? allowedDivisionSeasonIds = null;
        if (request.DivisionId.HasValue)
        {
            var divisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(
                request.SeasonId, cancellationToken);
            allowedDivisionSeasonIds = divisionSeasons
                .Where(ds => ds.DivisionId == request.DivisionId.Value)
                .Select(ds => ds.Id)
                .ToHashSet();
            if (allowedDivisionSeasonIds.Count == 0)
                return AssignFixtureDatesResponse.WithErrors(new[]
                {
                    "La división seleccionada no está asignada a esta temporada."
                });
        }

        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        if (allowedDivisionSeasonIds != null)
            fixtures = fixtures.Where(f => allowedDivisionSeasonIds.Contains(f.DivisionSeasonId)).ToList();

        if (fixtures.Count > 0)
        {
            var updated = ApplyDatesToFixtures(fixtures, request.FirstRoundDate, matchDays);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _draftStore.Clear(request.SeasonId);
            await TryNotifyBulkFixtureAsync(season, cancellationToken);
            return updated;
        }

        var draft = _draftStore.Get(request.SeasonId);
        if (draft == null || draft.Rounds.Count == 0)
            return AssignFixtureDatesResponse.WithErrors(new[]
            {
                "No hay fixtures para asignar fechas. Importá, copiá o generá el fixture primero."
            });

        return ApplyDatesToDraft(draft, request.SeasonId, request.FirstRoundDate, matchDays, allowedDivisionSeasonIds);
    }

    private static AssignFixtureDatesResponse ApplyDatesToFixtures(
        List<Fixture> fixtures,
        DateOnly firstRoundDate,
        IReadOnlyList<int> matchDays)
    {
        var roundNumbers = fixtures.Select(f => f.RoundNumber).Distinct().OrderBy(r => r).ToList();
        var dates = FixtureRoundDateCalculator.BuildRoundDates(firstRoundDate, matchDays, roundNumbers.Count);
        var dateByRound = roundNumbers
            .Select((round, index) => (round, date: dates[index]))
            .ToDictionary(x => x.round, x => x.date);

        foreach (var fixture in fixtures)
            fixture.SetDateAndTime(dateByRound[fixture.RoundNumber], fixture.StartTime);

        return new AssignFixtureDatesResponse(fixtures.Count, roundNumbers.Count, Array.Empty<string>());
    }

    private AssignFixtureDatesResponse ApplyDatesToDraft(
        FixtureDraftDto draft,
        Guid seasonId,
        DateOnly firstRoundDate,
        IReadOnlyList<int> matchDays,
        HashSet<Guid>? allowedDivisionSeasonIds)
    {
        var relevantRounds = draft.Rounds
            .Select(r => FilterDraftRound(r, allowedDivisionSeasonIds))
            .Where(r => r.Matches.Count > 0 || (r.ByeTeams?.Count ?? 0) > 0)
            .ToList();

        if (relevantRounds.Count == 0)
            return AssignFixtureDatesResponse.WithErrors(new[]
            {
                "No hay partidos en el borrador para el alcance seleccionado."
            });

        var roundNumbers = relevantRounds.Select(r => r.RoundNumber).Distinct().OrderBy(r => r).ToList();
        var dates = FixtureRoundDateCalculator.BuildRoundDates(firstRoundDate, matchDays, roundNumbers.Count);
        var dateByRound = roundNumbers
            .Select((round, index) => (round, date: dates[index]))
            .ToDictionary(x => x.round, x => x.date);

        var updatedMatchCount = 0;
        var updatedRounds = new List<FixtureDraftRoundDto>();

        foreach (var round in draft.Rounds)
        {
            if (allowedDivisionSeasonIds == null)
            {
                if (!dateByRound.TryGetValue(round.RoundNumber, out var dateAll))
                {
                    updatedRounds.Add(round);
                    continue;
                }

                var matchesAll = round.Matches.Select(m => WithDate(m, dateAll)).ToList();
                updatedMatchCount += matchesAll.Count;
                updatedRounds.Add(new FixtureDraftRoundDto(round.RoundNumber, dateAll, matchesAll, round.ByeTeams));
                continue;
            }

            var untouched = round.Matches
                .Where(m => !allowedDivisionSeasonIds.Contains(m.DivisionSeasonId))
                .ToList();
            var targeted = round.Matches
                .Where(m => allowedDivisionSeasonIds.Contains(m.DivisionSeasonId))
                .ToList();

            if (targeted.Count == 0)
            {
                updatedRounds.Add(round);
                continue;
            }

            if (!dateByRound.TryGetValue(round.RoundNumber, out var date))
            {
                updatedRounds.Add(round);
                continue;
            }

            var rewritten = targeted.Select(m => WithDate(m, date)).ToList();
            updatedMatchCount += rewritten.Count;

            // Keep untouched matches; if they had another date, leave them (may split grouping on get — acceptable).
            var combined = untouched.Concat(rewritten).ToList();
            var byesUntouched = (round.ByeTeams ?? Array.Empty<FixtureDraftByeDto>())
                .Where(b => !allowedDivisionSeasonIds.Contains(b.DivisionSeasonId))
                .ToList();
            var byesTargeted = (round.ByeTeams ?? Array.Empty<FixtureDraftByeDto>())
                .Where(b => allowedDivisionSeasonIds.Contains(b.DivisionSeasonId))
                .ToList();

            if (untouched.Count > 0 && round.MatchDate != date)
            {
                updatedRounds.Add(new FixtureDraftRoundDto(
                    round.RoundNumber, round.MatchDate, untouched, byesUntouched));
                updatedRounds.Add(new FixtureDraftRoundDto(
                    round.RoundNumber, date, rewritten, byesTargeted));
            }
            else
            {
                updatedRounds.Add(new FixtureDraftRoundDto(
                    round.RoundNumber, date, combined, byesUntouched.Concat(byesTargeted).ToList()));
            }
        }

        _draftStore.Set(seasonId, new FixtureDraftDto(updatedRounds));
        return new AssignFixtureDatesResponse(updatedMatchCount, roundNumbers.Count, Array.Empty<string>());
    }

    private static FixtureDraftRoundDto FilterDraftRound(
        FixtureDraftRoundDto round,
        HashSet<Guid>? allowedDivisionSeasonIds)
    {
        if (allowedDivisionSeasonIds == null)
            return round;

        return new FixtureDraftRoundDto(
            round.RoundNumber,
            round.MatchDate,
            round.Matches.Where(m => allowedDivisionSeasonIds.Contains(m.DivisionSeasonId)).ToList(),
            (round.ByeTeams ?? Array.Empty<FixtureDraftByeDto>())
                .Where(b => allowedDivisionSeasonIds.Contains(b.DivisionSeasonId))
                .ToList());
    }

    private static FixtureDraftMatchDto WithDate(FixtureDraftMatchDto m, DateOnly date) =>
        new(
            m.DivisionSeasonId,
            m.DivisionName,
            m.HomeTeamDivisionSeasonId,
            m.HomeTeamName,
            m.AwayTeamDivisionSeasonId,
            m.AwayTeamName,
            m.FieldId,
            m.FieldName,
            date,
            m.KickoffTime);

    private async Task TryNotifyBulkFixtureAsync(Season season, CancellationToken cancellationToken)
    {
        try
        {
            var league = await _leagueRepository.GetByIdAsync(season.LeagueId, cancellationToken);
            if (league == null) return;

            await _pushNotifications.NotifyFixtureUpdatedAsync(new FixtureUpdatedPushEvent
            {
                LeagueId = league.Id,
                LeagueSlug = league.Slug,
                LeagueName = league.Name,
                BulkAssign = true
            }, cancellationToken);
        }
        catch
        {
        }
    }
}
