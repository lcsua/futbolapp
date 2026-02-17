using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class User : Entity
    {
        public string FullName { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string GoogleSub { get; private set; }
        public string AvatarUrl { get; private set; }
        public UserRole Role { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsVerified { get; private set; }

        private readonly List<UserLeague> _userLeagues = new();
        public virtual IReadOnlyCollection<UserLeague> UserLeagues => _userLeagues.AsReadOnly();

        protected User() { }

        public User(string fullName, string email, string passwordHash = null, string googleSub = null, string avatarUrl = null)
        {
            FullName = !string.IsNullOrWhiteSpace(fullName) ? fullName : throw new ArgumentException("Full name required.", nameof(fullName));
            Email = !string.IsNullOrWhiteSpace(email) ? email : throw new ArgumentException("Email required.", nameof(email));
            PasswordHash = passwordHash;
            GoogleSub = googleSub;
            AvatarUrl = avatarUrl;
            Role = UserRole.USER;
            IsActive = true;
        }

        public void AssignRole(UserRole role)
        {
            Role = role;
            UpdateTimestamp();
        }
    }
}
