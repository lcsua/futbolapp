using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteNeverAssignedTeams
{
    public class DeleteNeverAssignedTeamsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public List<Guid> TeamIds { get; set; } = new();
    }

    public class DeleteNeverAssignedTeamsResponse
    {
        public int DeletedCount { get; }
        public DeleteNeverAssignedTeamsResponse(int deletedCount) => DeletedCount = deletedCount;
    }

    public interface IDeleteNeverAssignedTeamsUseCase
    {
        Task<DeleteNeverAssignedTeamsResponse> ExecuteAsync(DeleteNeverAssignedTeamsRequest request, CancellationToken cancellationToken = default);
    }

    public class DeleteNeverAssignedTeamsUseCase : IDeleteNeverAssignedTeamsUseCase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchIncidentRepository _matchIncidentRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNeverAssignedTeamsUseCase(
            ITeamRepository teamRepository,
            IMatchIncidentRepository matchIncidentRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchIncidentRepository = matchIncidentRepository ?? throw new ArgumentNullException(nameof(matchIncidentRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<DeleteNeverAssignedTeamsResponse> ExecuteAsync(DeleteNeverAssignedTeamsRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var ids = (request.TeamIds ?? new List<Guid>()).Distinct().ToList();
            if (ids.Count == 0)
                throw new BusinessException("Select at least one team to delete.");

            var deleted = 0;
            foreach (var teamId in ids)
            {
                var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
                if (team == null)
                    throw new KeyNotFoundException($"Team {teamId} not found.");
                if (team.LeagueId != request.LeagueId)
                    throw new ForbiddenAccessException("Team does not belong to this league.");

                if (await _teamRepository.HasAnySeasonAssignmentAsync(teamId, cancellationToken))
                {
                    throw new BusinessException(
                        $"Cannot delete team \"{team.DisplayName}\": it has been assigned to a season.");
                }

                if (await _matchIncidentRepository.ExistsByTeamIdAsync(teamId, cancellationToken))
                {
                    throw new BusinessException(
                        $"Cannot delete team \"{team.DisplayName}\": it has match incidents.");
                }

                await _teamRepository.RemoveAsync(team, cancellationToken);
                deleted++;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteNeverAssignedTeamsResponse(deleted);
        }
    }
}
