using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetUserLeagues
{
    public class GetUserLeaguesRequest
    {
        public Guid UserId { get; }

        public GetUserLeaguesRequest(Guid userId)
        {
            UserId = userId;
        }
    }

    public class GetUserLeaguesResponse
    {
        public List<LeagueDto> Leagues { get; }

        public GetUserLeaguesResponse(List<LeagueDto> leagues)
        {
            Leagues = leagues;
        }
    }

    public interface IGetUserLeaguesUseCase
    {
        Task<GetUserLeaguesResponse> ExecuteAsync(GetUserLeaguesRequest request, CancellationToken cancellationToken = default);
    }

    public class GetUserLeaguesUseCase : IGetUserLeaguesUseCase
    {
        private readonly ILeagueRepository _leagueRepository;

        public GetUserLeaguesUseCase(ILeagueRepository leagueRepository)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        }

        public async Task<GetUserLeaguesResponse> ExecuteAsync(GetUserLeaguesRequest request, CancellationToken cancellationToken = default)
        {
            // No specific Auth check needed here as we are fetching ONLY what the user has access to.
            var leagues = await _leagueRepository.GetByUserIdAsync(request.UserId, cancellationToken);
            var leagueDtos = leagues.ConvertAll(l => new LeagueDto(l.Id, l.Name, l.Country, l.Description, l.LogoUrl));
            return new GetUserLeaguesResponse(leagueDtos);
        }
    }
}
