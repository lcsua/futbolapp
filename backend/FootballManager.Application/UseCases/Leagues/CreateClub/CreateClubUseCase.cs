using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateClub
{
    public class CreateClubUseCase : ICreateClubUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateClubUseCase(
            ILeagueRepository leagueRepository,
            IClubRepository clubRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateClubResponse> ExecuteAsync(CreateClubRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Club name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var existing = await _clubRepository.GetByLeagueAndNameAsync(request.LeagueId, request.Name, cancellationToken);
            if (existing != null)
                throw new BusinessException($"A club named '{request.Name.Trim()}' already exists in this league.");

            var club = new Club(league, request.Name, request.LogoUrl);
            await _clubRepository.AddAsync(club, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateClubResponse(club.Id);
        }
    }
}
