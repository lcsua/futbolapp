using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetLeagueDivisions
{
    public class GetLeagueDivisionsUseCase : IGetLeagueDivisionsUseCase
    {
        private readonly IDivisionRepository _divisionRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetLeagueDivisionsUseCase(
            IDivisionRepository divisionRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetLeagueDivisionsResponse> ExecuteAsync(GetLeagueDivisionsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var divisions = await _divisionRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = divisions.Select(d => new DivisionDto(d.Id, d.LeagueId, d.Name, d.Description)).ToList();
            return new GetLeagueDivisionsResponse(dtos);
        }
    }
}
