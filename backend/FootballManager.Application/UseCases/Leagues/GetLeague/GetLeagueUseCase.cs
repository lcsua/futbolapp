using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetLeague
{
    public class GetLeagueUseCase : IGetLeagueUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueUseCase(ILeagueRepository leagueRepository, IUserLeagueRepository userLeagueRepository)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueResponse> ExecuteAsync(GetLeagueRequest request, CancellationToken cancellationToken = default)
        {
            // 1. Validate Access
            // Ensure the requesting user is a member of the target league before providing data.
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
            {
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");
            }

            // 2. Perform Operation
            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);

            if (league == null)
            {
                // Assuming NotFoundException exists or handle null. 
                // For now, returning null or throwing generic. 
                // I'll throw a KeyNotFoundException or similar if not found, to be safe.
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");
            }

            var leagueDto = new LeagueDto(league.Id, league.Name, league.Country, league.Description, league.LogoUrl);
            return new GetLeagueResponse(leagueDto);
        }
    }
}
