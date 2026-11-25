using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class LeagueChampionPhoto : Entity
    {
        public const int MaxCaptionLength = 300;
        public const int MaxImageUrlLength = 1000;

        public Guid LeagueChampionId { get; private set; }
        public virtual LeagueChampion LeagueChampion { get; private set; } = null!;

        public string ImageUrl { get; private set; } = string.Empty;
        public string? Caption { get; private set; }
        public int DisplayOrder { get; private set; }
        public bool IsCover { get; private set; }

        protected LeagueChampionPhoto() { }

        public LeagueChampionPhoto(
            LeagueChampion champion,
            string imageUrl,
            string? caption,
            int displayOrder,
            bool isCover)
        {
            LeagueChampion = champion ?? throw new ArgumentNullException(nameof(champion));
            LeagueChampionId = champion.Id;
            ImageUrl = RequireImageUrl(imageUrl);
            ApplyMetadata(caption, displayOrder, isCover);
        }

        public void ApplyMetadata(string? caption, int displayOrder, bool isCover)
        {
            if (displayOrder < 0)
                throw new ArgumentOutOfRangeException(nameof(displayOrder), "Display order cannot be negative.");

            var normalizedCaption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
            if (normalizedCaption != null && normalizedCaption.Length > MaxCaptionLength)
                throw new ArgumentException($"Caption cannot exceed {MaxCaptionLength} characters.", nameof(caption));

            Caption = normalizedCaption;
            DisplayOrder = displayOrder;
            IsCover = isCover;
            UpdateTimestamp();
        }

        private static string RequireImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                throw new ArgumentException("Image URL cannot be empty.", nameof(imageUrl));

            var trimmed = imageUrl.Trim();
            if (trimmed.Length > MaxImageUrlLength)
                throw new ArgumentException($"Image URL cannot exceed {MaxImageUrlLength} characters.", nameof(imageUrl));

            return trimmed;
        }
    }
}
