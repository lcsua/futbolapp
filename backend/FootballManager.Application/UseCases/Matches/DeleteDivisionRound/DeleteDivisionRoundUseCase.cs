using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.DeleteDivisionRound;

public sealed class DeleteDivisionRoundUseCase : IDeleteDivisionRoundUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDivisionRoundUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DeleteDivisionRoundResponse> ExecuteAsync(
        DeleteDivisionRoundRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        if (!DeleteDivisionRoundConfirmation.IsValid(request.Confirmation))
            throw new BusinessException(
                $"Para eliminar la fecha, la confirmación tiene que ser {DeleteDivisionRoundConfirmation.Word}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        var divisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var divisionSeasonByDivisionId = divisionSeasons.ToDictionary(ds => ds.DivisionId);

        var wanted = new HashSet<(Guid DivisionSeasonId, int Round)>();
        foreach (var target in request.Targets ?? new List<DeleteDivisionRoundTarget>())
        {
            if (target == null || target.DivisionId == Guid.Empty)
                continue;
            if (!divisionSeasonByDivisionId.TryGetValue(target.DivisionId, out var divisionSeason))
                throw new KeyNotFoundException("La división no está asignada a esta temporada.");
            if (divisionSeason.Division != null && divisionSeason.Division.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Division does not belong to this league.");

            foreach (var round in target.Rounds ?? new List<int>())
            {
                if (round < 1)
                    throw new BusinessException("La fecha tiene que ser mayor o igual a 1.");
                wanted.Add((divisionSeason.Id, round));
            }
        }

        if (wanted.Count == 0)
            throw new BusinessException("Elegí al menos una división y una fecha.");

        var fixtures = (await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken))
            .Where(f => wanted.Contains((f.DivisionSeasonId, f.RoundNumber)))
            .ToList();

        if (fixtures.Count == 0)
            throw new BusinessException("No hay partidos para eliminar en esa selección.");

        var resultsRemoved = fixtures.Count(f => f.Result != null || f.Status != MatchStatus.SCHEDULED);
        foreach (var fixture in fixtures)
            _fixtureRepository.Remove(fixture);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeleteDivisionRoundResponse(fixtures.Count, resultsRemoved);
    }
}
