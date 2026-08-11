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
        public string Nickname { get; private set; }
        public string Document { get; private set; }
        public DateOnly? BirthDate { get; private set; }
        public int? JerseyNumber { get; private set; }
        public PlayerPosition? Position { get; private set; }
        public string Phone { get; private set; }
        public string Email { get; private set; }
        public string Nationality { get; private set; }
        public int? HeightCm { get; private set; }
        public int? WeightKg { get; private set; }
        public string PhotoUrl { get; private set; }
        public bool IsActive { get; private set; }

        public string DisplayName =>
            !string.IsNullOrWhiteSpace(Nickname)
                ? Nickname
                : $"{FirstName} {LastName}".Trim();

        protected Player() { }

        public Player(
            Team team,
            string firstName,
            string lastName,
            string? nickname = null,
            string? document = null,
            DateOnly? birthDate = null,
            PlayerPosition? position = null)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
            TeamId = team.Id;
            FirstName = !string.IsNullOrWhiteSpace(firstName)
                ? firstName.Trim()
                : throw new ArgumentException("First name required.", nameof(firstName));
            LastName = !string.IsNullOrWhiteSpace(lastName)
                ? lastName.Trim()
                : throw new ArgumentException("Last name required.", nameof(lastName));
            Nickname = nickname?.Trim() ?? string.Empty;
            Document = document?.Trim() ?? string.Empty;
            BirthDate = birthDate;
            Position = position;
            Phone = string.Empty;
            Email = string.Empty;
            Nationality = string.Empty;
            PhotoUrl = string.Empty;
            IsActive = true;
        }

        public void UpdateIdentity(
            string firstName,
            string lastName,
            string? nickname,
            string? document,
            DateOnly? birthDate)
        {
            FirstName = !string.IsNullOrWhiteSpace(firstName)
                ? firstName.Trim()
                : throw new ArgumentException("First name required.", nameof(firstName));
            LastName = !string.IsNullOrWhiteSpace(lastName)
                ? lastName.Trim()
                : throw new ArgumentException("Last name required.", nameof(lastName));
            Nickname = nickname?.Trim() ?? string.Empty;
            Document = document?.Trim() ?? string.Empty;
            BirthDate = birthDate;
            UpdateTimestamp();
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
            Phone = phone ?? string.Empty;
            Email = email ?? string.Empty;
            UpdateTimestamp();
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            SetActive(false);
        }
    }
}
