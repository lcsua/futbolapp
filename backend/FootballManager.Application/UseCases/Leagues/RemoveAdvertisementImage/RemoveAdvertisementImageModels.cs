using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.RemoveAdvertisementImage
{
    public interface IRemoveAdvertisementImageUseCase
    {
        Task<RemoveAdvertisementImageResponse> ExecuteAsync(RemoveAdvertisementImageRequest request, CancellationToken cancellationToken = default);
    }

    public class RemoveAdvertisementImageRequest
    {
        public Guid LeagueId { get; set; }
        public Guid AdvertisementId { get; set; }
        public Guid UserId { get; set; }
        public AdvertisementImageKind Kind { get; set; }
    }

    public class RemoveAdvertisementImageResponse
    {
        public AdvertisementDto Advertisement { get; }
        public string? PreviousImageUrl { get; }

        public RemoveAdvertisementImageResponse(AdvertisementDto advertisement, string? previousImageUrl)
        {
            Advertisement = advertisement;
            PreviousImageUrl = previousImageUrl;
        }
    }
}
