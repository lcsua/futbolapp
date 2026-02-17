using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateTeam
{
    public class UpdateTeamUseCase : IUpdateTeamUseCase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeamUseCase(
            ITeamRepository teamRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Team name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
            if (team == null)
                throw new KeyNotFoundException($"Team {request.TeamId} not found.");
            if (team.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Team does not belong to this league.");

            team.UpdateName(request.Name, request.ShortName);
            team.UpdateDetails(request.PrimaryColor, request.SecondaryColor, request.LogoUrl, request.Email, request.PhotoUrl);
            team.SetDelegateInfo(request.DelegateName, request.DelegateContact);
            team.SetFoundedYear(request.FoundedYear);
            _teamRepository.Update(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
