using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;

namespace FootballManager.Application.UseCases.Leagues.DiscardSeasonFixtureDraft;

public interface IDiscardSeasonFixtureDraftUseCase
{
    Task ExecuteAsync(Guid leagueId, Guid seasonId, Guid userId, CancellationToken cancellationToken = default);
}

public sealed class DiscardSeasonFixtureDraftUseCase : IDiscardSeasonFixtureDraftUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureDraftStore _draftStore;

    public DiscardSeasonFixtureDraftUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureDraftStore draftStore)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
    }

    public async Task ExecuteAsync(Guid leagueId, Guid seasonId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var season = await _seasonRepository.GetByIdAsync(seasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {seasonId} not found.");
        if (season.LeagueId != leagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        _draftStore.Clear(seasonId);
    }
}
