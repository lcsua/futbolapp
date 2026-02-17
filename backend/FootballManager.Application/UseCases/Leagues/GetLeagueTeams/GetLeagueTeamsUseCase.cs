using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueTeams
{
    public class GetLeagueTeamsRequest
    {
        public Guid LeagueId { get; }
        public Guid UserId { get; }

        public GetLeagueTeamsRequest(Guid leagueId, Guid userId)
        {
            LeagueId = leagueId;
            UserId = userId;
        }
    }

    public class GetLeagueTeamsResponse
    {
        public List<TeamDto> Teams { get; }

        public GetLeagueTeamsResponse(List<TeamDto> teams)
        {
            Teams = teams;
        }
    }

    public interface IGetLeagueTeamsUseCase
    {
        Task<GetLeagueTeamsResponse> ExecuteAsync(GetLeagueTeamsRequest request, CancellationToken cancellationToken = default);
    }

    public class GetLeagueTeamsUseCase : IGetLeagueTeamsUseCase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueTeamsUseCase(ITeamRepository teamRepository, IUserLeagueRepository userLeagueRepository)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueTeamsResponse> ExecuteAsync(GetLeagueTeamsRequest request, CancellationToken cancellationToken = default)
        {
            // Authorization Check: Only members can view teams
            if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            {
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");
            }

            var teams = await _teamRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var teamDtos = teams.ConvertAll(t => new TeamDto(t.Id, t.Name, t.ShortName, t.LogoUrl, t.Email, t.FoundedYear, t.DelegateName, t.DelegateContact, t.PhotoUrl));
            return new GetLeagueTeamsResponse(teamDtos);
        }
    }
}
