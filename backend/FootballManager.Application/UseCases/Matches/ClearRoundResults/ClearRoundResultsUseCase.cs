using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.ClearRoundResults;

public sealed class ClearRoundResultsUseCase : IClearRoundResultsUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IMatchIncidentRepository _matchIncidentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ClearRoundResultsUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IResultRepository resultRepository,
        IMatchIncidentRepository matchIncidentRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
        _matchIncidentRepository = matchIncidentRepository ?? throw new ArgumentNullException(nameof(matchIncidentRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ClearRoundResultsResponse> ExecuteAsync(
        ClearRoundResultsRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        if (request.Round < 1)
            throw new BusinessException("La fecha (round) debe ser mayor o igual a 1.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(
            request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new KeyNotFoundException("La división no está asignada a esta temporada.");

        var fixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
            request.SeasonId, divisionSeason.Id, request.Round, cancellationToken);

        var cleared = 0;
        foreach (var fixture in fixtures)
        {
            var hadResultOrCompleted = false;

            var result = await _resultRepository.GetByFixtureIdAsync(fixture.Id, cancellationToken);
            if (result != null)
            {
                _resultRepository.Remove(result);
                hadResultOrCompleted = true;
            }

            await _matchIncidentRepository.DeleteByFixtureIdAsync(fixture.Id, cancellationToken);

            if (fixture.Status != MatchStatus.SCHEDULED)
            {
                fixture.ChangeStatus(MatchStatus.SCHEDULED);
                hadResultOrCompleted = true;
            }

            if (hadResultOrCompleted)
                cleared++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ClearRoundResultsResponse(cleared);
    }
}
