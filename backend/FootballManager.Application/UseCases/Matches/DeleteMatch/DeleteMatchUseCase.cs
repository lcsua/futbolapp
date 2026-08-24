using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Matches.DeleteMatch;

public sealed class DeleteMatchUseCase : IDeleteMatchUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMatchUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureRepository fixtureRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(matchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Match {matchId} not found.");
        if (fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        var season = fixture.Season ?? await _seasonRepository.GetByIdAsync(fixture.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {fixture.SeasonId} not found.");
        SeasonGuard.EnsureOpen(season);

        _fixtureRepository.Remove(fixture);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
