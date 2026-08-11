using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Matches.SwapDivisionHomeAway;

public sealed class SwapDivisionHomeAwayUseCase : ISwapDivisionHomeAwayUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SwapDivisionHomeAwayUseCase(
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

    public async Task<SwapDivisionHomeAwayResponse> ExecuteAsync(
        SwapDivisionHomeAwayRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(
            request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new KeyNotFoundException("La división no está asignada a esta temporada.");

        var fixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
            request.SeasonId, divisionSeason.Id, round: null, cancellationToken);

        foreach (var fixture in fixtures)
        {
            fixture.SwapHomeAway();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new SwapDivisionHomeAwayResponse(fixtures.Count);
    }
}
