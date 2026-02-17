using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public class CreateLeagueUseCase : ICreateLeagueUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateLeagueUseCase(
            ILeagueRepository leagueRepository,
            IUserLeagueRepository userLeagueRepository,
            IUserRepository userRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateLeagueResponse> ExecuteAsync(CreateLeagueRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("League name required");

            if (await _leagueRepository.ExistsByNameAsync(request.Name, cancellationToken))
                throw new LeagueAlreadyExistsException(request.Name);

            var creator = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (creator == null)
                throw new KeyNotFoundException($"User {request.UserId} not found.");

            var league = new League(request.Name, request.Country, request.Description, request.LogoUrl);
            await _leagueRepository.AddAsync(league, cancellationToken);

            var userLeague = new UserLeague(creator, league, UserRole.ADMIN);
            await _userLeagueRepository.AddAsync(userLeague, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateLeagueResponse(league.Id);
        }
    }
}
