using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class LeagueDocumentCategory : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; } = null!;

        public string Name { get; private set; } = string.Empty;
        public string Slug { get; private set; } = string.Empty;
        public int SortOrder { get; private set; }
        public bool RequiresDocumentDate { get; private set; }
        public bool IsActive { get; private set; }

        protected LeagueDocumentCategory() { }

        public LeagueDocumentCategory(
            League league,
            string name,
            string slug,
            int sortOrder = 0,
            bool requiresDocumentDate = false,
            bool isActive = true)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Category name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.Trim().ToLowerInvariant() : throw new ArgumentException("Category slug cannot be empty.", nameof(slug));
            SortOrder = sortOrder;
            RequiresDocumentDate = requiresDocumentDate;
            IsActive = isActive;
        }

        public void Update(string name, string slug, int sortOrder, bool requiresDocumentDate, bool isActive)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Category name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.Trim().ToLowerInvariant() : throw new ArgumentException("Category slug cannot be empty.", nameof(slug));
            SortOrder = sortOrder;
            RequiresDocumentDate = requiresDocumentDate;
            IsActive = isActive;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }
    }
}
