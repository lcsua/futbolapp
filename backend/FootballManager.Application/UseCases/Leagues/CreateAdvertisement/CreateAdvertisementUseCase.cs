using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateAdvertisement
{
    public class CreateAdvertisementUseCase : ICreateAdvertisementUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly IAdvertisementRepository _advertisementRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateAdvertisementUseCase(
            ILeagueRepository leagueRepository,
            IAdvertisementRepository advertisementRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _advertisementRepository = advertisementRepository ?? throw new ArgumentNullException(nameof(advertisementRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateAdvertisementResponse> ExecuteAsync(CreateAdvertisementRequest request, CancellationToken cancellationToken = default)
        {
            Validate(request);

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            Advertisement advertisement;
            try
            {
                advertisement = new Advertisement(
                    league,
                    request.Name,
                    request.AdvertiserName,
                    request.Slot,
                    desktopImageUrl: null,
                    mobileImageUrl: null,
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

            await _advertisementRepository.AddAsync(advertisement, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateAdvertisementResponse(advertisement.Id);
        }

        private static void Validate(CreateAdvertisementRequest request)
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
