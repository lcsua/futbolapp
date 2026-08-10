using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetNeverAssignedTeams
{
    public class GetNeverAssignedTeamsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetNeverAssignedTeamsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }

    public class GetNeverAssignedTeamsResponse
    {
        public List<TeamDto> Teams { get; }

        public GetNeverAssignedTeamsResponse(List<TeamDto> teams)
        {
            Teams = teams ?? new List<TeamDto>();
        }
    }

    public interface IGetNeverAssignedTeamsUseCase
    {
        Task<GetNeverAssignedTeamsResponse> ExecuteAsync(GetNeverAssignedTeamsRequest request, CancellationToken cancellationToken = default);
    }

    public class GetNeverAssignedTeamsUseCase : IGetNeverAssignedTeamsUseCase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetNeverAssignedTeamsUseCase(ITeamRepository teamRepository, IUserLeagueRepository userLeagueRepository)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetNeverAssignedTeamsResponse> ExecuteAsync(GetNeverAssignedTeamsRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var teams = await _teamRepository.GetNeverAssignedByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = teams.ConvertAll(t => new TeamDto(
                t.Id,
                t.Name,
                t.Suffix,
                t.DisplayName,
                t.ShortName,
                t.LogoUrl,
                t.Email,
                t.FoundedYear,
                t.DelegateName,
                t.DelegateContact,
                t.PhotoUrl,
                t.ClubId,
                t.Club?.Name));
            return new GetNeverAssignedTeamsResponse(dtos);
        }
    }
}
