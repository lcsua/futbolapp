using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.ReopenSeason;

public sealed class ReopenSeasonUseCase : IReopenSeasonUseCase
{
    private readonly ISeasonRepository _seasonRepository;
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenSeasonUseCase(
        ISeasonRepository seasonRepository,
        IUserLeagueRepository userLeagueRepository,
        IUnitOfWork unitOfWork)
    {
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(ReopenSeasonRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        if (season.IsActive)
            return;

        var leagueSeasons = await _seasonRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var hasNewerActive = leagueSeasons.Any(s =>
            s.Id != season.Id
            && s.IsActive
            && (s.StartDate > season.StartDate
                || (s.StartDate == season.StartDate && s.CreatedAt > season.CreatedAt)));

        if (hasNewerActive)
            throw new BusinessException(
                "Cannot reopen this season: a newer season is already active. Close the newer season first.");

        season.Activate();
        _seasonRepository.Update(season);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
