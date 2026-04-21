using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class League : Entity
    {
        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string Description { get; private set; }
        public string Country { get; private set; }
        public string LogoUrl { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPublic { get; private set; }

        private readonly List<Season> _seasons = new();
        public virtual IReadOnlyCollection<Season> Seasons => _seasons.AsReadOnly();

        private readonly List<Division> _divisions = new();
        public virtual IReadOnlyCollection<Division> Divisions => _divisions.AsReadOnly();

        private readonly List<Team> _teams = new();
        public virtual IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();

        private readonly List<Club> _clubs = new();
        public virtual IReadOnlyCollection<Club> Clubs => _clubs.AsReadOnly();

        private readonly List<Field> _fields = new();
        public virtual IReadOnlyCollection<Field> Fields => _fields.AsReadOnly();

        private readonly List<UserLeague> _userLeagues = new();
        public virtual IReadOnlyCollection<UserLeague> UserLeagues => _userLeagues.AsReadOnly();

        protected League() { } // For EF Core

        public League(string name, string country, string slug, string description = null, string logoUrl = null, bool isPublic = false, bool isActive = true)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("League name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("League slug cannot be empty.", nameof(slug));

            Name = name;
            Country = country;
            Slug = slug.ToLowerInvariant();
            Description = description;
            LogoUrl = logoUrl;
            IsActive = isActive;
            IsPublic = isPublic;
        }

        public void UpdateDetails(string name, string country, string slug, string description, string logoUrl, bool isPublic, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("League name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("League slug cannot be empty.", nameof(slug));

            Name = name;
            Country = country;
            Slug = slug.ToLowerInvariant();
            Description = description;
            LogoUrl = logoUrl;
            IsPublic = isPublic;
            IsActive = isActive;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public Season AddSeason(string name, DateOnly startDate, DateOnly? endDate)
        {
            var season = new Season(this, name, startDate, endDate);
            _seasons.Add(season);
            return season;
        }
    }
}
