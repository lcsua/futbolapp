using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.SetAdvertisementImage
{
    public class SetAdvertisementImageUseCase : ISetAdvertisementImageUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SetAdvertisementImageUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<SetAdvertisementImageResponse> ExecuteAsync(
            SetAdvertisementImageRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.ImageUrl))
                throw new ArgumentException("Image URL is required.");
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
            try
            {
                if (request.Kind == AdvertisementImageKind.Desktop)
                {
                    previousImageUrl = advertisement.DesktopImageUrl;
                    advertisement.SetDesktopImage(request.ImageUrl);
                }
                else
                {
                    previousImageUrl = advertisement.MobileImageUrl;
                    advertisement.SetMobileImage(request.ImageUrl);
                }
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            _advertisementRepository.Update(advertisement);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new SetAdvertisementImageResponse(AdvertisementDto.From(advertisement), previousImageUrl);
        }
    }
}
