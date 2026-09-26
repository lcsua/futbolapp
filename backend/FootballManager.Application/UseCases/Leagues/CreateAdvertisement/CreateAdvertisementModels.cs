using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.CreateAdvertisement
{
    public interface ICreateAdvertisementUseCase
    {
        Task<CreateAdvertisementResponse> ExecuteAsync(CreateAdvertisementRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateAdvertisementRequest
    {
        public Guid LeagueId { get; set; }
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

    public class CreateAdvertisementResponse
    {
        public Guid Id { get; }

        public CreateAdvertisementResponse(Guid id)
        {
            Id = id;
        }
    }
}
