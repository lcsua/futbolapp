using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.AssignDivisionToSeason
{
    public class AssignDivisionToSeasonUseCase : IAssignDivisionToSeasonUseCase
    {
        private readonly ISeasonRepository _seasonRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IFixtureRepository _fixtureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AssignDivisionToSeasonUseCase(
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IFixtureRepository fixtureRepository,
            IUnitOfWork unitOfWork)
        {
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<AssignDivisionToSeasonResponse> ExecuteAsync(AssignDivisionToSeasonRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            var fixtureCount = await _fixtureRepository.CountBySeasonIdAsync(request.SeasonId, cancellationToken);
            if (fixtureCount > 0)
                throw new BusinessException("Cannot modify divisions: fixtures have been committed. Season is locked.");

            var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken);
            if (division == null)
                throw new KeyNotFoundException($"Division {request.DivisionId} not found.");
            if (division.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Division does not belong to this league.");

            var existing = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(request.SeasonId, request.DivisionId, cancellationToken);
            if (existing != null)
                return new AssignDivisionToSeasonResponse(existing.Id);

            var divisionSeason = new DivisionSeason(season, division);
            await _divisionSeasonRepository.AddAsync(divisionSeason, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new AssignDivisionToSeasonResponse(divisionSeason.Id);
        }
    }
}
