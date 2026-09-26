using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.GetAdvertisements;

namespace FootballManager.Application.UseCases.Leagues.GetAdvertisement
{
    public interface IGetAdvertisementUseCase
    {
        Task<AdvertisementDto> ExecuteAsync(GetAdvertisementRequest request, CancellationToken cancellationToken = default);
    }

    public class GetAdvertisementRequest
    {
        public Guid LeagueId { get; set; }
        public Guid AdvertisementId { get; set; }
        public Guid UserId { get; set; }
    }
}
