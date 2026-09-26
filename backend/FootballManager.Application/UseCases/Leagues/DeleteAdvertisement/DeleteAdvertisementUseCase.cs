using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteAdvertisement
{
    public class DeleteAdvertisementUseCase : IDeleteAdvertisementUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteAdvertisementUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<DeleteAdvertisementResponse> ExecuteAsync(DeleteAdvertisementRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var advertisement = await _advertisementRepository.GetByIdAsync(request.AdvertisementId, cancellationToken);
            if (advertisement == null)
                throw new KeyNotFoundException($"Advertisement {request.AdvertisementId} not found.");
            if (advertisement.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Advertisement does not belong to this league.");

            var desktopImageUrl = advertisement.DesktopImageUrl;
            var mobileImageUrl = advertisement.MobileImageUrl;

            advertisement.SoftDelete();
            _advertisementRepository.Update(advertisement);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAdvertisementResponse(desktopImageUrl, mobileImageUrl);
        }
    }
}
