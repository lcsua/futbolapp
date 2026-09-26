namespace FootballManager.Domain.Entities
{
    public class RolePermission
    {
        public Guid RoleId { get; private set; }
        public virtual Role Role { get; private set; } = null!;

        public Guid PermissionId { get; private set; }
        public virtual Permission Permission { get; private set; } = null!;

        protected RolePermission() { }

        public RolePermission(Role role, Permission permission)
        {
            Role = role ?? throw new ArgumentNullException(nameof(role));
            RoleId = role.Id;
            Permission = permission ?? throw new ArgumentNullException(nameof(permission));
            PermissionId = permission.Id;
        }
    }
}
