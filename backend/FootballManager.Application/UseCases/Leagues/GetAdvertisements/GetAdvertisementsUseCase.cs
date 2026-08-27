using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetAdvertisements
{
    public class GetAdvertisementsUseCase : IGetAdvertisementsUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetAdvertisementsUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetAdvertisementsResponse> ExecuteAsync(GetAdvertisementsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var advertisements = await _advertisementRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = advertisements.Select(AdvertisementDto.From).ToList();
            return new GetAdvertisementsResponse(dtos);
        }
    }
}
