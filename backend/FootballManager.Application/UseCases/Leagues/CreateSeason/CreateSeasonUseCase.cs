using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateSeason
{
    public class CreateSeasonUseCase : ICreateSeasonUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateSeasonUseCase(
            ILeagueRepository leagueRepository,
            ISeasonRepository seasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateSeasonResponse> ExecuteAsync(CreateSeasonRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Season name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var season = new Season(league, request.Name, request.StartDate, request.EndDate);
            await _seasonRepository.AddAsync(season, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateSeasonResponse(season.Id);
        }
    }
}
