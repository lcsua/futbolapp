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

namespace FootballManager.Application.UseCases.Leagues.CopyFixturesFromSeason;

public sealed class CopyFixturesFromSeasonUseCase : ICopyFixturesFromSeasonUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;

    public CopyFixturesFromSeasonUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CopyFixturesFromSeasonResponse> ExecuteAsync(
        CopyFixturesFromSeasonRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        if (request.SourceSeasonId == request.TargetSeasonId)
            throw new BusinessException("Source and target season must be different.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
        if (league == null)
            throw new KeyNotFoundException($"League {request.LeagueId} not found.");

        var targetSeason = await _seasonRepository.GetByIdAsync(request.TargetSeasonId, cancellationToken);
        if (targetSeason == null || targetSeason.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Target season {request.TargetSeasonId} not found.");

        SeasonGuard.EnsureOpen(targetSeason);

        var sourceSeason = await _seasonRepository.GetByIdAsync(request.SourceSeasonId, cancellationToken);
        if (sourceSeason == null || sourceSeason.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Source season {request.SourceSeasonId} not found.");

        var sourceFixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SourceSeasonId, cancellationToken);
        if (request.DivisionId.HasValue)
            sourceFixtures = sourceFixtures
                .Where(f => f.DivisionSeason?.DivisionId == request.DivisionId.Value)
                .ToList();

        if (sourceFixtures.Count == 0)
            return CopyFixturesFromSeasonResponse.WithErrors(new[]
            {
                request.DivisionId.HasValue
                    ? "La temporada de origen no tiene fixtures en esa división."
                    : "La temporada de origen no tiene fixtures para copiar."
            });

        var targetDivisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(
            request.TargetSeasonId, cancellationToken);
        var targetByDivisionId = targetDivisionSeasons.ToDictionary(ds => ds.DivisionId);

        var errors = new List<string>();
        var planned = new List<(DivisionSeason TargetDs, TeamDivisionSeason Home, TeamDivisionSeason Away, int Round)>();

        foreach (var src in sourceFixtures.OrderBy(f => f.RoundNumber).ThenBy(f => f.DivisionSeason.Division.Name))
        {
            var divisionId = src.DivisionSeason.DivisionId;
            var divisionName = src.DivisionSeason.Division?.Name ?? divisionId.ToString();
            var homeTeam = src.HomeTeamDivisionSeason?.Team;
            var awayTeam = src.AwayTeamDivisionSeason?.Team;
            var homeTeamId = homeTeam?.Id ?? src.HomeTeamDivisionSeason?.TeamId;
            var awayTeamId = awayTeam?.Id ?? src.AwayTeamDivisionSeason?.TeamId;

            if (!targetByDivisionId.TryGetValue(divisionId, out var targetDs))
            {
                errors.Add($"Ronda {src.RoundNumber}: la división \"{divisionName}\" no está asignada a la temporada destino.");
                continue;
            }

            var targetTeamsById = targetDs.TeamAssignments
                .Where(t => t.Team != null)
                .ToDictionary(t => t.TeamId);

            if (homeTeamId == null || !targetTeamsById.TryGetValue(homeTeamId.Value, out var homeTds))
            {
                errors.Add(
                    $"Ronda {src.RoundNumber} ({divisionName}): el equipo \"{homeTeam?.Name ?? "?"}\" no está asignado en la temporada destino.");
                continue;
            }

            if (awayTeamId == null || !targetTeamsById.TryGetValue(awayTeamId.Value, out var awayTds))
            {
                errors.Add(
                    $"Ronda {src.RoundNumber} ({divisionName}): el equipo \"{awayTeam?.Name ?? "?"}\" no está asignado en la temporada destino.");
                continue;
            }

            var newHome = request.InvertHomes ? awayTds : homeTds;
            var newAway = request.InvertHomes ? homeTds : awayTds;
            planned.Add((targetDs, newHome, newAway, src.RoundNumber));
        }

        if (errors.Count > 0)
            return CopyFixturesFromSeasonResponse.WithErrors(errors);

        var affectedDivisionSeasonIds = planned.Select(p => p.TargetDs.Id).Distinct().ToList();
        foreach (var dsId in affectedDivisionSeasonIds)
            await _fixtureRepository.RemoveByDivisionSeasonIdAsync(dsId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var item in planned)
        {
            var fixture = new Fixture(
                league,
                targetSeason,
                item.TargetDs,
                item.Home,
                item.Away,
                item.Round);
            await _fixtureRepository.AddAsync(fixture, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _draftStore.Clear(request.TargetSeasonId);

        return new CopyFixturesFromSeasonResponse(planned.Count, Array.Empty<string>());
    }
}
