using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.RoundInterzonalMatches;

public interface IRoundInterzonalMatchesUseCase
{
    Task<RoundInterzonalMatchesDto> GetAsync(
        Guid leagueId, Guid seasonId, int round, IReadOnlyList<Guid> divisionIds, Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the pending interzonal matches of the round among the given zones with <paramref name="pairs"/>.
    /// Interzonal matches with a result must be kept.
    /// </summary>
    Task<RoundInterzonalMatchesDto> SetAsync(
        Guid leagueId, Guid seasonId, int round, IReadOnlyList<Guid> divisionIds,
        IReadOnlyList<SetRoundInterzonalPairDto> pairs, Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed class RoundInterzonalMatchesUseCase : IRoundInterzonalMatchesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IRoundSlotAssignerFactory _slotAssignerFactory;
    private readonly IUnitOfWork _unitOfWork;

    public RoundInterzonalMatchesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFieldRepository fieldRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IRoundSlotAssignerFactory slotAssignerFactory,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _slotAssignerFactory = slotAssignerFactory ?? throw new ArgumentNullException(nameof(slotAssignerFactory));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    private sealed record RoundContext(
        Season Season,
        IReadOnlyList<DivisionSeason> SeasonDivisions,
        IReadOnlyList<DivisionSeason> Zones,
        IReadOnlyDictionary<Guid, TeamDivisionSeason> TdsById,
        IReadOnlyList<Fixture> SeasonFixtures,
        IReadOnlyList<Fixture> RoundFixtures,
        IReadOnlyList<Fixture> Interzonal,
        IReadOnlySet<Guid> BusyTeamIds,
        DateOnly? RoundDate);

    public async Task<RoundInterzonalMatchesDto> GetAsync(
        Guid leagueId, Guid seasonId, int round, IReadOnlyList<Guid> divisionIds, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAsync(leagueId, seasonId, round, divisionIds, userId, cancellationToken);
        return ToDto(round, ctx, Array.Empty<string>());
    }

    public async Task<RoundInterzonalMatchesDto> SetAsync(
        Guid leagueId, Guid seasonId, int round, IReadOnlyList<Guid> divisionIds,
        IReadOnlyList<SetRoundInterzonalPairDto> pairs, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ctx = await LoadAsync(leagueId, seasonId, round, divisionIds, userId, cancellationToken);
        SeasonGuard.EnsureOpen(ctx.Season);

        if (_draftStore.Get(seasonId) != null)
            throw new BusinessException("Hay un borrador de fixture sin guardar. Guardalo o descartalo antes de editar interzonales.");

        var zoneOf = ctx.Zones
            .SelectMany(z => z.TeamAssignments.Select(t => (t.Id, z.Id)))
            .ToDictionary(x => x.Item1, x => x.Item2);

        var used = new HashSet<Guid>();
        foreach (var p in pairs)
        {
            if (!zoneOf.TryGetValue(p.HomeTeamDivisionSeasonId, out var homeZone) || !zoneOf.TryGetValue(p.AwayTeamDivisionSeasonId, out var awayZone))
                throw new BusinessException("Todos los equipos tienen que ser de las zonas elegidas.");
            if (homeZone == awayZone)
                throw new BusinessException("Un interzonal tiene que cruzar equipos de zonas distintas.");
            foreach (var team in new[] { p.HomeTeamDivisionSeasonId, p.AwayTeamDivisionSeasonId })
            {
                var name = ctx.TdsById[team].Team.CompetitionName;
                if (ctx.BusyTeamIds.Contains(team))
                    throw new BusinessException($"{name} ya juega otro partido en la fecha {round}.");
                if (!used.Add(team))
                    throw new BusinessException($"{name} aparece en más de un interzonal de la fecha {round}.");
            }
        }

        static (Guid, Guid) Key(Guid a, Guid b) => a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        var requestedKeys = pairs.Select(p => Key(p.HomeTeamDivisionSeasonId, p.AwayTeamDivisionSeasonId)).ToHashSet();

        var locked = ctx.Interzonal.Where(IsLocked).ToList();
        var missingLocked = locked.FirstOrDefault(f => !requestedKeys.Contains(Key(f.HomeTeamDivisionSeasonId, f.AwayTeamDivisionSeasonId)));
        if (missingLocked != null)
            throw new BusinessException(
                $"No se puede quitar {missingLocked.HomeTeamDivisionSeason.Team.CompetitionName} vs {missingLocked.AwayTeamDivisionSeason.Team.CompetitionName}: ya tiene resultado.");

        var kept = ctx.Interzonal
            .Where(f => IsLocked(f) || pairs.Any(p => p.HomeTeamDivisionSeasonId == f.HomeTeamDivisionSeasonId && p.AwayTeamDivisionSeasonId == f.AwayTeamDivisionSeasonId))
            .ToList();
        var keptKeys = kept.Select(f => Key(f.HomeTeamDivisionSeasonId, f.AwayTeamDivisionSeasonId)).ToHashSet();
        var removed = ctx.Interzonal.Except(kept).ToList();
        var toCreate = pairs.Where(p => !keptKeys.Contains(Key(p.HomeTeamDivisionSeasonId, p.AwayTeamDivisionSeasonId))).ToList();

        var warnings = new List<string>();
        if (removed.Count == 0 && toCreate.Count == 0)
            return ToDto(round, ctx, warnings);

        var league = await _leagueRepository.GetByIdAsync(leagueId, cancellationToken)
            ?? throw new KeyNotFoundException($"League {leagueId} not found.");
        var fieldById = (await _fieldRepository.GetByLeagueIdAsync(leagueId, cancellationToken)).ToDictionary(f => f.Id);
        var divisionSeasonById = ctx.SeasonDivisions.ToDictionary(ds => ds.Id);

        var newMatches = toCreate
            .Select(p =>
            {
                var home = ctx.TdsById[p.HomeTeamDivisionSeasonId];
                return (Ds: divisionSeasonById[home.DivisionSeasonId], Home: home, Away: ctx.TdsById[p.AwayTeamDivisionSeasonId]);
            })
            .ToList();

        IReadOnlyList<(Guid FieldId, TimeOnly Start)>? slots = null;
        if (ctx.RoundDate is DateOnly date && newMatches.Count > 0)
        {
            var assigner = await _slotAssignerFactory.CreateAsync(leagueId, ctx.SeasonDivisions, cancellationToken);
            slots = await assigner.AssignAsync(date, newMatches, ctx.SeasonFixtures.Except(removed));
            if (slots == null)
                warnings.Add("No alcanzaron las canchas/horarios de esa fecha: los interzonales nuevos quedaron sin cancha ni hora.");
        }
        else if (newMatches.Count > 0)
        {
            warnings.Add("La fecha no tiene día asignado: los interzonales nuevos quedaron sin día, cancha ni hora.");
        }

        await _fixtureRepository.RemoveRangeAsync(removed.Select(f => f.Id), cancellationToken);
        for (var i = 0; i < newMatches.Count; i++)
        {
            var (ds, home, away) = newMatches[i];
            var field = slots != null ? fieldById.GetValueOrDefault(slots[i].FieldId) : null;
            await _fixtureRepository.AddAsync(new Fixture(
                league, ctx.Season, ds, home, away, round,
                ctx.RoundDate,
                field != null ? slots![i].Start : null,
                field), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await LoadAsync(leagueId, seasonId, round, divisionIds, userId, cancellationToken);
        return ToDto(round, refreshed, warnings);
    }

    private static bool IsLocked(Fixture f) => f.Status != MatchStatus.SCHEDULED || f.Result != null;

    private async Task<RoundContext> LoadAsync(
        Guid leagueId, Guid seasonId, int round, IReadOnlyList<Guid> divisionIds, Guid userId,
        CancellationToken cancellationToken)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var season = await _seasonRepository.GetByIdAsync(seasonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Season {seasonId} not found.");
        if (season.LeagueId != leagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");
        if (round < 1)
            throw new BusinessException("La fecha tiene que ser 1 o mayor.");

        var ids = divisionIds.Distinct().ToList();
        if (ids.Count < 2)
            throw new BusinessException("Elegí al menos dos zonas.");

        var seasonDivisions = await _divisionSeasonRepository.GetBySeasonIdAsync(seasonId, cancellationToken);
        var zones = seasonDivisions.Where(ds => ids.Contains(ds.DivisionId)).OrderBy(ds => ds.Division.Name).ToList();
        if (zones.Count != ids.Count)
            throw new BusinessException("Alguna de las zonas elegidas no está asignada a esta temporada.");

        var zoneTeamIds = zones.SelectMany(z => z.TeamAssignments).Select(t => t.Id).ToHashSet();
        var tdsById = seasonDivisions.SelectMany(ds => ds.TeamAssignments).ToDictionary(t => t.Id);

        var seasonFixtures = await _fixtureRepository.GetBySeasonIdAsync(seasonId, cancellationToken);
        var roundFixtures = seasonFixtures.Where(f => f.RoundNumber == round).ToList();
        var interzonal = roundFixtures
            .Where(f => zoneTeamIds.Contains(f.HomeTeamDivisionSeasonId)
                        && zoneTeamIds.Contains(f.AwayTeamDivisionSeasonId)
                        && FixtureDraftMapper.IsInterzonal(f))
            .ToList();
        var busy = roundFixtures
            .Except(interzonal)
            .SelectMany(f => new[] { f.HomeTeamDivisionSeasonId, f.AwayTeamDivisionSeasonId })
            .ToHashSet();
        var roundDate = roundFixtures.Where(f => f.MatchDate.HasValue).Select(f => f.MatchDate).Min();

        return new RoundContext(season, seasonDivisions, zones, tdsById, seasonFixtures, roundFixtures, interzonal, busy, roundDate);
    }

    private static RoundInterzonalMatchesDto ToDto(int round, RoundContext ctx, IReadOnlyList<string> warnings)
    {
        var zones = ctx.Zones
            .Select(z => new RoundInterzonalZoneDto(
                z.Id,
                z.DivisionId,
                z.Division.Name,
                z.TeamAssignments
                    .OrderBy(t => t.Team.CompetitionName)
                    .Select(t => new RoundInterzonalTeamDto(t.Id, t.Team.CompetitionName, ctx.BusyTeamIds.Contains(t.Id)))
                    .ToList()))
            .ToList();

        var pairs = ctx.Interzonal
            .OrderBy(f => f.StartTime ?? TimeOnly.MaxValue)
            .Select(f => new RoundInterzonalPairDto(
                f.Id,
                f.HomeTeamDivisionSeasonId,
                f.HomeTeamDivisionSeason.Team.CompetitionName,
                f.AwayTeamDivisionSeasonId,
                f.AwayTeamDivisionSeason.Team.CompetitionName,
                IsLocked(f),
                f.Field?.Name,
                f.StartTime))
            .ToList();

        return new RoundInterzonalMatchesDto(round, ctx.RoundDate, zones, pairs, warnings);
    }
}
