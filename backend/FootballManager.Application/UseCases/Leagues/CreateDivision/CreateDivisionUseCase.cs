using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateDivision
{
    public class CreateDivisionUseCase : ICreateDivisionUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDivisionUseCase(
            ILeagueRepository leagueRepository,
            IDivisionRepository divisionRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateDivisionResponse> ExecuteAsync(CreateDivisionRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Division name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var division = new Division(league, request.Name, request.Description);
            await _divisionRepository.AddAsync(division, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateDivisionResponse(division.Id);
        }
    }
}
