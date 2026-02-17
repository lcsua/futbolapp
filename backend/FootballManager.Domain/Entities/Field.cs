using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Field : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public string? Address { get; private set; }
        public string? City { get; private set; }
        public double? GeoLat { get; private set; }
        public double? GeoLng { get; private set; }
        public bool IsAvailable { get; private set; }
        public string? Description { get; private set; }

        protected Field() { }

        public Field(League league, string name)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Field name cannot be empty.", nameof(name));
            Address = null;
            City = null;
            Description = null;
            IsAvailable = true;
        }

        public void SetLocation(string? address, string? city, double? lat, double? lng)
        {
            Address = address;
            City = city;
            GeoLat = lat;
            GeoLng = lng;
            UpdateTimestamp();
        }

        public void SetDescription(string? description)
        {
            Description = description;
            UpdateTimestamp();
        }

        public void SetAvailability(bool isAvailable)
        {
            IsAvailable = isAvailable;
            UpdateTimestamp();
        }

        public void UpdateDetails(string name, string? address, string? city, double? geoLat, double? geoLng, bool isAvailable, string? description)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Field name cannot be empty.", nameof(name));
            Address = address;
            City = city;
            GeoLat = geoLat;
            GeoLng = geoLng;
            IsAvailable = isAvailable;
            Description = description;
            UpdateTimestamp();
        }
    }
}
