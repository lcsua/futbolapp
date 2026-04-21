using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Club : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public string LogoUrl { get; private set; }

        protected Club() { }

        public Club(League league, string name, string? logoUrl = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Club name cannot be empty.", nameof(name));
            LogoUrl = logoUrl ?? string.Empty;
        }

        public void Update(string name, string? logoUrl = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Club name cannot be empty.", nameof(name));
            LogoUrl = logoUrl ?? string.Empty;
            UpdateTimestamp();
        }
    }
}
