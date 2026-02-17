using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateTeam
{
    public class CreateTeamUseCase : ICreateTeamUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeamUseCase(
            ILeagueRepository leagueRepository,
            ITeamRepository teamRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateTeamResponse> ExecuteAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Team name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var team = new Team(league, request.Name, request.ShortName, request.Email);
            await _teamRepository.AddAsync(team, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTeamResponse(team.Id);
        }
    }
}
