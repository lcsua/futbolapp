using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.CloseSeason;

public sealed class CloseSeasonUseCase : ICloseSeasonUseCase
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseSeasonUseCase(
        ISeasonRepository seasonRepository,
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IUnitOfWork unitOfWork)
    {
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CloseSeasonResponse> ExecuteAsync(CloseSeasonRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var fixtures = await _fixtureRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        var pendingResults = fixtures.Count(f =>
            f.Status != MatchStatus.COMPLETED && f.Status != MatchStatus.PLAYED);

        if (season.IsActive)
        {
            season.Close(DateOnly.FromDateTime(DateTime.UtcNow));
            _seasonRepository.Update(season);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new CloseSeasonResponse(pendingResults);
    }
}
