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
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.CommitSeasonFixtures;

public sealed class CommitSeasonFixturesUseCase : ICommitSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;

    public CommitSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFieldRepository fieldRepository,
        ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(CommitSeasonFixturesRequest request, CancellationToken cancellationToken = default)
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

        var draft = _draftStore.Get(request.SeasonId);
        if (draft == null || draft.Rounds.Count == 0)
            throw new BusinessException("No fixture draft to commit. Generate fixtures first.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
        if (league == null)
            throw new KeyNotFoundException($"League {request.LeagueId} not found.");

        if (draft.Replan != null)
        {
            await CommitReplanAsync(league, season, draft, cancellationToken);
            return;
        }

        await _fixtureRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var seasonDivisions = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var divisionSeasonById = seasonDivisions.ToDictionary(ds => ds.Id);
        var tdsById = seasonDivisions.SelectMany(ds => ds.TeamAssignments).ToDictionary(t => t.Id);

        foreach (var round in draft.Rounds)
        {
            foreach (var m in round.Matches)
            {
                if (!divisionSeasonById.TryGetValue(m.DivisionSeasonId, out var divisionSeason)) continue;
                if (!tdsById.TryGetValue(m.HomeTeamDivisionSeasonId, out var homeTds)) continue;
                if (!tdsById.TryGetValue(m.AwayTeamDivisionSeasonId, out var awayTds)) continue;

                if (!m.FieldId.HasValue || !m.Date.HasValue || !m.KickoffTime.HasValue) continue;

                var field = await _fieldRepository.GetByIdAsync(m.FieldId.Value, cancellationToken);
                if (field == null) continue;

                var fixture = new Fixture(
                    league,
                    season,
                    divisionSeason,
                    homeTds,
                    awayTds,
                    round.RoundNumber,
                    m.Date.Value,
                    m.KickoffTime.Value,
                    field);

                await _fixtureRepository.AddAsync(fixture, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _draftStore.Clear(request.SeasonId);
    }

    /// <summary>Removes only the pending matches that were re-planned and inserts the new ones.</summary>
    private async Task CommitReplanAsync(League league, Season season, FixtureDraftDto draft, CancellationToken cancellationToken)
    {
        var replan = draft.Replan!;
        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(season.Id, cancellationToken);
        var fixtureById = fixtures.ToDictionary(f => f.Id);

        var changed = replan.RemovedFixtureIds.Any(id =>
            !fixtureById.TryGetValue(id, out var f) || f.Status != MatchStatus.SCHEDULED || f.Result != null);
        if (changed)
            throw new BusinessException("El fixture cambió desde que se generó la replanificación (se cargó un resultado o se borró un partido). Volvé a replanificar.");

        var seasonDivisions = await _divisionSeasonRepository.GetBySeasonIdAsync(season.Id, cancellationToken);
        var divisionSeasonById = seasonDivisions.ToDictionary(ds => ds.Id);
        var tdsById = seasonDivisions.SelectMany(ds => ds.TeamAssignments).ToDictionary(t => t.Id);
        var fieldById = (await _fieldRepository.GetByLeagueIdAsync(league.Id, cancellationToken)).ToDictionary(f => f.Id);

        var newFixtures = new List<Fixture>();
        foreach (var round in draft.Rounds)
        {
            foreach (var m in round.Matches.Where(m => m.IsReplanned))
            {
                if (!divisionSeasonById.TryGetValue(m.DivisionSeasonId, out var divisionSeason)
                    || !tdsById.TryGetValue(m.HomeTeamDivisionSeasonId, out var homeTds)
                    || !tdsById.TryGetValue(m.AwayTeamDivisionSeasonId, out var awayTds))
                {
                    throw new BusinessException("Cambiaron los equipos de las zonas desde que se generó la replanificación. Volvé a replanificar.");
                }

                var field = m.FieldId.HasValue ? fieldById.GetValueOrDefault(m.FieldId.Value) : null;
                newFixtures.Add(new Fixture(
                    league,
                    season,
                    divisionSeason,
                    homeTds,
                    awayTds,
                    round.RoundNumber,
                    m.Date ?? round.MatchDate,
                    field != null ? m.KickoffTime : null,
                    field));
            }
        }

        await _fixtureRepository.RemoveRangeAsync(replan.RemovedFixtureIds, cancellationToken);
        foreach (var fixture in newFixtures)
            await _fixtureRepository.AddAsync(fixture, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _draftStore.Clear(season.Id);
    }
}
