using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.UpdateAdvertisement
{
    public interface IUpdateAdvertisementUseCase
    {
        Task ExecuteAsync(UpdateAdvertisementRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateAdvertisementRequest
    {
        public Guid LeagueId { get; set; }
        public Guid AdvertisementId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AdvertiserName { get; set; } = string.Empty;
        public string? TargetUrl { get; set; }
        public AdvertisementSlot Slot { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
