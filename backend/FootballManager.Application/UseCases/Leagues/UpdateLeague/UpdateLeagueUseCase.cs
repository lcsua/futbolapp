using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateLeague
{
    public class UpdateLeagueUseCase : IUpdateLeagueUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateLeagueUseCase(
            ILeagueRepository leagueRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateLeagueRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("League name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            league.UpdateDetails(request.Name, request.Country, request.Description ?? string.Empty, request.LogoUrl ?? string.Empty);
            _leagueRepository.Update(league);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
