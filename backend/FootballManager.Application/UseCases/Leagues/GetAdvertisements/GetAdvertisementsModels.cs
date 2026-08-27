using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Leagues.GetAdvertisements
{
    public interface IGetAdvertisementsUseCase
    {
        Task<GetAdvertisementsResponse> ExecuteAsync(GetAdvertisementsRequest request, CancellationToken cancellationToken = default);
    }

    public class GetAdvertisementsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
    }

    public class GetAdvertisementsResponse
    {
        public List<AdvertisementDto> Advertisements { get; }

        public GetAdvertisementsResponse(List<AdvertisementDto> advertisements)
        {
            Advertisements = advertisements ?? new List<AdvertisementDto>();
        }
    }

    public record AdvertisementDto(
        Guid Id,
        Guid LeagueId,
        string Name,
        string AdvertiserName,
        string? DesktopImageUrl,
        string? MobileImageUrl,
        string? TargetUrl,
        AdvertisementSlot Slot,
        DateTime? StartsAt,
        DateTime? EndsAt,
        int Priority,
        bool IsActive,
        DateTime CreatedAt,
        DateTime UpdatedAt)
    {
        public static AdvertisementDto From(Advertisement advertisement)
        {
            return new AdvertisementDto(
                advertisement.Id,
                advertisement.LeagueId,
                advertisement.Name,
                advertisement.AdvertiserName,
                advertisement.DesktopImageUrl,
                advertisement.MobileImageUrl,
                advertisement.TargetUrl,
                advertisement.Slot,
                advertisement.StartsAt,
                advertisement.EndsAt,
                advertisement.Priority,
                advertisement.IsActive,
                advertisement.CreatedAt,
                advertisement.UpdatedAt);
        }
    }
}
