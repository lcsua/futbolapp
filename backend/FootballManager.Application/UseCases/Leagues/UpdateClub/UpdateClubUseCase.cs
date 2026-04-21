using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateClub
{
    public class UpdateClubUseCase : IUpdateClubUseCase
    {
        private readonly IClubRepository _clubRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateClubUseCase(
            IClubRepository clubRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateClubRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Club name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var club = await _clubRepository.GetByIdAsync(request.ClubId, cancellationToken);
            if (club == null)
                throw new KeyNotFoundException($"Club {request.ClubId} not found.");
            if (club.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Club does not belong to this league.");

            var sameNameClub = await _clubRepository.GetByLeagueAndNameAsync(request.LeagueId, request.Name, cancellationToken);
            if (sameNameClub != null && sameNameClub.Id != request.ClubId)
                throw new BusinessException($"A club named '{request.Name.Trim()}' already exists in this league.");

            club.Update(request.Name, request.LogoUrl);
            _clubRepository.Update(club);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
