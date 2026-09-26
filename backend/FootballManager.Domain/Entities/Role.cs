using FootballManager.Domain.Authorization;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Role : Entity
    {
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string? Code { get; private set; }
        public bool IsSystem { get; private set; }
        public Guid? LeagueId { get; private set; }
        public virtual League? League { get; private set; }

        private readonly List<RolePermission> _rolePermissions = new();
        public virtual IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

        protected Role() { }

        public Role(string name, string description, Guid? leagueId = null, string? code = null, bool isSystem = false)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Name required.", nameof(name));
            Description = description?.Trim() ?? string.Empty;
            LeagueId = leagueId;
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
            IsSystem = isSystem;
        }

        public void UpdateDetails(string name, string description)
        {
            if (IsSystem && Code == RoleCodes.Admin)
                throw new InvalidOperationException("The administrator role cannot be renamed.");

            Name = !string.IsNullOrWhiteSpace(name) ? name.Trim() : throw new ArgumentException("Name required.", nameof(name));
            Description = description?.Trim() ?? string.Empty;
            UpdateTimestamp();
        }

        public void ReplacePermissions(IEnumerable<Permission> permissions)
        {
            if (IsSystem && Code == RoleCodes.Admin)
                throw new InvalidOperationException("The administrator role always has all permissions.");

            _rolePermissions.Clear();
            foreach (var permission in permissions.DistinctBy(p => p.Id))
            {
                _rolePermissions.Add(new RolePermission(this, permission));
            }
            UpdateTimestamp();
        }

        public bool IsAdminRole => IsSystem && string.Equals(Code, RoleCodes.Admin, StringComparison.OrdinalIgnoreCase);
    }
}
