using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Push;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.ShiftFixtureDates;

public sealed class ShiftFixtureDatesUseCase : IShiftFixtureDatesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPushNotificationService _pushNotifications;
    private readonly ILeagueRepository _leagueRepository;

    public ShiftFixtureDatesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork,
        IPushNotificationService pushNotifications,
        ILeagueRepository leagueRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _pushNotifications = pushNotifications ?? throw new ArgumentNullException(nameof(pushNotifications));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
    }

    public async Task<ShiftFixtureDatesResponse> ExecuteAsync(
        ShiftFixtureDatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");

        SeasonGuard.EnsureOpen(season);

        if (request.FromDate == default)
            return ShiftFixtureDatesResponse.WithErrors(new[]
            {
                "Elegí la fecha desde la cual correr los partidos."
            });

        var draft = _draftStore.Get(request.SeasonId);
        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);

        if (draft != null && fixtures.Count > 0)
            return ShiftFixtureDatesResponse.WithErrors(new[]
            {
                "Hay un borrador de fixture sin guardar. Guardalo o descartalo antes de correr las fechas."
            });

        if (fixtures.Count > 0)
            return await ShiftPersistedAsync(season, fixtures, request.FromDate, cancellationToken);

        if (draft == null || draft.Rounds.Count == 0)
            return ShiftFixtureDatesResponse.WithErrors(new[]
            {
                "No hay fixtures para correr. Importá, copiá o generá el fixture primero."
            });

        var shifted = FixtureDateShifter.ShiftDraft(draft, request.FromDate, out var draftUpdated);
        if (draftUpdated == 0)
            return NothingToShift(skipped: 0);

        _draftStore.Set(request.SeasonId, shifted);
        return new ShiftFixtureDatesResponse(draftUpdated, 0, Array.Empty<string>());
    }

    private async Task<ShiftFixtureDatesResponse> ShiftPersistedAsync(
        Season season,
        List<Fixture> fixtures,
        DateOnly fromDate,
        CancellationToken cancellationToken)
    {
        var updated = 0;
        var skipped = 0;

        foreach (var fixture in fixtures)
        {
            var matchDate = fixture.MatchDate;
            if (matchDate is null || matchDate.Value < fromDate)
                continue;

            if (!FixtureDateShifter.ShouldShift(matchDate, fromDate, fixture.Result != null, fixture.Status))
            {
                skipped++;
                continue;
            }

            fixture.SetDateAndTime(FixtureDateShifter.Shift(matchDate.Value), fixture.StartTime);
            updated++;
        }

        if (updated == 0)
            return NothingToShift(skipped);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await TryNotifyBulkFixtureAsync(season, cancellationToken);
        return new ShiftFixtureDatesResponse(updated, skipped, Array.Empty<string>());
    }

    private static ShiftFixtureDatesResponse NothingToShift(int skipped)
    {
        if (skipped > 0)
            return ShiftFixtureDatesResponse.WithErrors(new[]
            {
                "Los partidos desde esa fecha ya se jugaron, están en curso o fueron cancelados. No se movió ninguno."
            });

        return ShiftFixtureDatesResponse.WithErrors(new[]
        {
            "No hay partidos con fecha desde el día elegido."
        });
    }

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
