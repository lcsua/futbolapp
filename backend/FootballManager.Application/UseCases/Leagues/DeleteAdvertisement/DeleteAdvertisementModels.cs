using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteAdvertisement
{
    public interface IDeleteAdvertisementUseCase
    {
        Task<DeleteAdvertisementResponse> ExecuteAsync(DeleteAdvertisementRequest request, CancellationToken cancellationToken = default);
    }

    public class DeleteAdvertisementRequest
    {
        public Guid LeagueId { get; set; }
        public Guid AdvertisementId { get; set; }
        public Guid UserId { get; set; }
    }

    public class DeleteAdvertisementResponse
    {
        public string? DesktopImageUrl { get; }
        public string? MobileImageUrl { get; }

        public DeleteAdvertisementResponse(string? desktopImageUrl, string? mobileImageUrl)
        {
            DesktopImageUrl = desktopImageUrl;
            MobileImageUrl = mobileImageUrl;
        }
    }
}
