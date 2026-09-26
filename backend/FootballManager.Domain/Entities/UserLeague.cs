using FootballManager.Domain.Authorization;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class UserLeague : Entity
    {
        public Guid UserId { get; private set; }
        public virtual User User { get; private set; } = null!;

        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; } = null!;

        public UserRole AssignedRole { get; private set; }

        public Guid? RoleId { get; private set; }
        public virtual Role? Role { get; private set; }

        protected UserLeague() { }

        public UserLeague(User user, League league, UserRole role)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            UserId = user.Id;
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            AssignedRole = role;
        }

        public UserLeague(User user, League league, Role role)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));
            UserId = user.Id;
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            AssignRole(role);
        }

        public void AssignRole(Role role)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            RoleId = role.Id;
            AssignedRole = role.IsAdminRole || string.Equals(role.Code, RoleCodes.Admin, StringComparison.OrdinalIgnoreCase)
                ? UserRole.ADMIN
                : UserRole.USER;
            UpdateTimestamp();
        }
    }
}
