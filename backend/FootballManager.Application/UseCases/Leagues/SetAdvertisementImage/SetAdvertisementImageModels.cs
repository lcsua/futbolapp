using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.SetAdvertisementImage
{
    public interface ISetAdvertisementImageUseCase
    {
        Task<SetAdvertisementImageResponse> ExecuteAsync(SetAdvertisementImageRequest request, CancellationToken cancellationToken = default);
    }

    public class SetAdvertisementImageRequest
    {
        public Guid LeagueId { get; set; }
        public Guid AdvertisementId { get; set; }
        public Guid UserId { get; set; }
        public AdvertisementImageKind Kind { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class SetAdvertisementImageResponse
    {
        public AdvertisementDto Advertisement { get; }
        public string? PreviousImageUrl { get; }

        public SetAdvertisementImageResponse(AdvertisementDto advertisement, string? previousImageUrl)
        {
            Advertisement = advertisement;
            PreviousImageUrl = previousImageUrl;
        }
    }
}
