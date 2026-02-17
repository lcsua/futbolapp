using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class Player : Entity
    {
        public Guid TeamId { get; private set; }
        public virtual Team Team { get; private set; }

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Document { get; private set; }
        public DateOnly BirthDate { get; private set; }
        public int? JerseyNumber { get; private set; }
        public PlayerPosition? Position { get; private set; } // Enum mapped to string
        public string Phone { get; private set; }
        public string Email { get; private set; }
        public string Nationality { get; private set; }
        public int? HeightCm { get; private set; }
        public int? WeightKg { get; private set; }
        public string PhotoUrl { get; private set; }
        public bool IsActive { get; private set; }

        protected Player() { }

        public Player(Team team, string firstName, string lastName, DateOnly birthDate, string document)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
            TeamId = team.Id;
            FirstName = !string.IsNullOrWhiteSpace(firstName) ? firstName : throw new ArgumentException("First name required.", nameof(firstName));
            LastName = !string.IsNullOrWhiteSpace(lastName) ? lastName : throw new ArgumentException("Last name required.", nameof(lastName));
            BirthDate = birthDate;
            Document = document;
            IsActive = true;
        }

        public void UpdateDetails(int? jerseyNumber, PlayerPosition? position, int? heightCm, int? weightKg)
        {
            JerseyNumber = jerseyNumber;
            Position = position;
            HeightCm = heightCm;
            WeightKg = weightKg;
            UpdateTimestamp();
        }

        public void UpdateContactInfo(string phone, string email)
        {
            Phone = phone;
            Email = email;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }
    }
}
