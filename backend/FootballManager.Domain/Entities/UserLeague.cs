using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class UserLeague : Entity
    {
        public Guid UserId { get; private set; }
        public virtual User User { get; private set; }

        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public UserRole AssignedRole { get; private set; }

        protected UserLeague() { }

        public UserLeague(User user, League league, UserRole role)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            UserId = user.Id;
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            AssignedRole = role;
        }
    }
}
