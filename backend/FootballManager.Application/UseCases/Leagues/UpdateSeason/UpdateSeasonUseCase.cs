using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateSeason
{
    public class UpdateSeasonUseCase : IUpdateSeasonUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateSeasonUseCase(
            ISeasonRepository seasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateSeasonRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Season name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            season.UpdateDetails(request.Name, request.StartDate, request.EndDate);
            _seasonRepository.Update(season);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
