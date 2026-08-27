using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.RemoveAdvertisementImage
{
    public class RemoveAdvertisementImageUseCase : IRemoveAdvertisementImageUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RemoveAdvertisementImageUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<RemoveAdvertisementImageResponse> ExecuteAsync(
            RemoveAdvertisementImageRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!Enum.IsDefined(request.Kind))
                throw new ArgumentException("Image kind is invalid.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var advertisement = await _advertisementRepository.GetByIdAsync(request.AdvertisementId, cancellationToken);
            if (advertisement == null)
                throw new KeyNotFoundException($"Advertisement {request.AdvertisementId} not found.");
            if (advertisement.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Advertisement does not belong to this league.");

            string? previousImageUrl;
            if (request.Kind == AdvertisementImageKind.Desktop)
            {
                previousImageUrl = advertisement.DesktopImageUrl;
                advertisement.ClearDesktopImage();
            }
            else
            {
                previousImageUrl = advertisement.MobileImageUrl;
                advertisement.ClearMobileImage();
            }

            _advertisementRepository.Update(advertisement);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new RemoveAdvertisementImageResponse(AdvertisementDto.From(advertisement), previousImageUrl);
        }
    }
}
