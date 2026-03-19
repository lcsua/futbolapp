using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Division : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string Description { get; private set; }

        protected Division() { }

        public Division(League league, string name, string slug, string description = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Division name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.ToLowerInvariant() : throw new ArgumentException("Division slug cannot be empty.", nameof(slug));
            Description = description;
        }

        public void UpdateDetails(string name, string slug, string description = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Division name cannot be empty.", nameof(name));
            Slug = !string.IsNullOrWhiteSpace(slug) ? slug.ToLowerInvariant() : throw new ArgumentException("Division slug cannot be empty.", nameof(slug));
            Description = description;
            UpdateTimestamp();
        }
    }
}
