using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateAdvertisement
{
    public class UpdateAdvertisementUseCase : IUpdateAdvertisementUseCase
    {
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateAdvertisementUseCase(
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateAdvertisementRequest request, CancellationToken cancellationToken = default)
        {
            Validate(request);

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var advertisement = await _advertisementRepository.GetByIdAsync(request.AdvertisementId, cancellationToken);
            if (advertisement == null)
                throw new KeyNotFoundException($"Advertisement {request.AdvertisementId} not found.");
            if (advertisement.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Advertisement does not belong to this league.");

            try
            {
                advertisement.Update(
                    request.Name,
                    request.AdvertiserName,
                    request.Slot,
                    request.TargetUrl,
                    request.StartsAt,
                    request.EndsAt,
                    request.Priority,
                    request.IsActive);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            _advertisementRepository.Update(advertisement);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private static void Validate(UpdateAdvertisementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Advertisement name is required.");
            if (string.IsNullOrWhiteSpace(request.AdvertiserName))
                throw new ArgumentException("Advertiser name is required.");
            if (!Enum.IsDefined(request.Slot))
                throw new ArgumentException("Slot is invalid.");
            if (request.Priority < 0)
                throw new ArgumentException("Priority must be greater than or equal to 0.");
            if (request.StartsAt.HasValue && request.EndsAt.HasValue && request.EndsAt.Value < request.StartsAt.Value)
                throw new ArgumentException("EndsAt must be greater than or equal to StartsAt.");
        }
    }
}
