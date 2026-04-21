using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueClubs
{
    public class GetLeagueClubsUseCase : IGetLeagueClubsUseCase
    {
        private readonly IClubRepository _clubRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueClubsUseCase(IClubRepository clubRepository, IUserLeagueRepository userLeagueRepository)
        {
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueClubsResponse> ExecuteAsync(GetLeagueClubsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var clubs = await _clubRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = clubs.Select(c => new ClubDto(c.Id, c.Name, c.LogoUrl)).ToList();
            return new GetLeagueClubsResponse(dtos);
        }
    }
}
