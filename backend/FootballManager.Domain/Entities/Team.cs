using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Team : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public string ShortName { get; private set; }
        public string PrimaryColor { get; private set; }
        public string SecondaryColor { get; private set; }
        public int? FoundedYear { get; private set; }
        public string DelegateName { get; private set; }
        public string DelegateContact { get; private set; }
        public string? Email { get; private set; }
        public string LogoUrl { get; private set; }
        public string PhotoUrl { get; private set; }

        private readonly List<Player> _players = new();
        public virtual IReadOnlyCollection<Player> Players => _players.AsReadOnly();

        protected Team() { }

        public Team(League league, string name, string shortName = null, string email = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Team name cannot be empty.", nameof(name));
            ShortName = shortName ?? string.Empty;
            PrimaryColor = string.Empty;
            SecondaryColor = string.Empty;
            Email = email;
            LogoUrl = string.Empty;
            DelegateName = string.Empty;
            DelegateContact = string.Empty;
            PhotoUrl = string.Empty;
        }

        public void UpdateName(string name, string shortName = null)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Team name cannot be empty.", nameof(name));
            ShortName = shortName ?? string.Empty;
            UpdateTimestamp();
        }

        public void UpdateDetails(string primaryColor, string secondaryColor, string logoUrl, string email = null, string photoUrl = null)
        {
            PrimaryColor = primaryColor ?? string.Empty;
            SecondaryColor = secondaryColor ?? string.Empty;
            LogoUrl = logoUrl ?? string.Empty;
            Email = email;
            PhotoUrl = photoUrl ?? string.Empty;
            UpdateTimestamp();
        }

        public void SetDelegateInfo(string name, string contact)
        {
            DelegateName = name ?? string.Empty;
            DelegateContact = contact ?? string.Empty;
            UpdateTimestamp();
        }

        public void SetFoundedYear(int? year)
        {
            FoundedYear = year;
            UpdateTimestamp();
        }

        public Player AddPlayer(string firstName, string lastName, DateOnly birthDate, string document)
        {
            var player = new Player(this, firstName, lastName, birthDate, document);
            _players.Add(player);
            return player;
        }
    }
}
