using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;

namespace FootballManager.Application.UseCases.Leagues.GetAdvertisement
{
    public class GetAdvertisementUseCase : IGetAdvertisementUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetAdvertisementUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<AdvertisementDto> ExecuteAsync(GetAdvertisementRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var advertisement = await _advertisementRepository.GetByIdAsync(request.AdvertisementId, cancellationToken);
            if (advertisement == null)
                throw new KeyNotFoundException($"Advertisement {request.AdvertisementId} not found.");
            if (advertisement.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Advertisement does not belong to this league.");

            return AdvertisementDto.From(advertisement);
        }
    }
}
