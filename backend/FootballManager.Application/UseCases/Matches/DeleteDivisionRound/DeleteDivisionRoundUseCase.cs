using System;
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
    private readonly IDivisionRepository _divisionRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDivisionRoundUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionRepository divisionRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
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

        if (request.Round < 1)
            throw new BusinessException("La fecha tiene que ser mayor o igual a 1.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken);
        if (division == null || division.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Division {request.DivisionId} not found or does not belong to this league.");

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(
            request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new KeyNotFoundException("La división no está asignada a esta temporada.");

        // Only matches owned by this division. An interzonal stored on another zone stays there.
        var fixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
            request.SeasonId, divisionSeason.Id, request.Round, cancellationToken);

        if (fixtures.Count == 0)
            throw new BusinessException($"La división \"{division.Name}\" no tiene partidos en la fecha {request.Round}.");

        var resultsRemoved = fixtures.Count(f => f.Result != null || f.Status != MatchStatus.SCHEDULED);
        foreach (var fixture in fixtures)
            _fixtureRepository.Remove(fixture);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeleteDivisionRoundResponse(fixtures.Count, resultsRemoved);
    }
}
