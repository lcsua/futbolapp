using System.Linq;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.LeagueChampions
{
    internal static class LeagueChampionMapper
    {
        public static LeagueChampionListItemDto ToListItem(LeagueChampion champion)
        {
            var photos = champion.Photos.Where(p => p.DeletedAt == null).ToList();
            var cover = photos.FirstOrDefault(p => p.IsCover);
            var logo = NormalizeLogo(champion.Team?.EffectiveLogoUrl);

            return new LeagueChampionListItemDto
            {
                Id = champion.Id,
                TeamId = champion.TeamId,
                TeamName = champion.Team?.CompetitionName ?? string.Empty,
                TeamLogoUrl = logo,
                TeamIsArchived = champion.Team?.DeletedAt != null,
                Year = champion.Year,
                ChampionshipName = champion.ChampionshipName,
                DivisionName = champion.DivisionName,
                SeasonId = champion.SeasonId,
                DivisionId = champion.DivisionId,
                WonAt = champion.WonAt,
                PhotoCount = photos.Count,
                CoverImageUrl = cover?.ImageUrl,
                CoverThumbUrl = ChampionImageUrls.ThumbUrl(cover?.ImageUrl),
            };
        }

        public static LeagueChampionDetailDto ToDetail(LeagueChampion champion)
        {
            var list = ToListItem(champion);
            var photos = champion.Photos
                .Where(p => p.DeletedAt == null)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.CreatedAt)
                .Select(p => new LeagueChampionPhotoDto
                {
                    Id = p.Id,
                    ImageUrl = p.ImageUrl,
                    ThumbUrl = ChampionImageUrls.ThumbUrl(p.ImageUrl),
                    Caption = p.Caption,
                    DisplayOrder = p.DisplayOrder,
                    IsCover = p.IsCover,
                })
                .ToList();

            return new LeagueChampionDetailDto
            {
                Id = list.Id,
                TeamId = list.TeamId,
                TeamName = list.TeamName,
                TeamLogoUrl = list.TeamLogoUrl,
                TeamIsArchived = list.TeamIsArchived,
                Year = list.Year,
                ChampionshipName = list.ChampionshipName,
                DivisionName = list.DivisionName,
                SeasonId = list.SeasonId,
                DivisionId = list.DivisionId,
                WonAt = list.WonAt,
                PhotoCount = list.PhotoCount,
                CoverImageUrl = list.CoverImageUrl,
                CoverThumbUrl = list.CoverThumbUrl,
                Notes = champion.Notes,
                Photos = photos,
            };
        }

        private static string? NormalizeLogo(string? logoUrl)
            => string.IsNullOrWhiteSpace(logoUrl) ? null : logoUrl;
    }
}
