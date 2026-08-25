using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly FootballManagerDbContext _context;

        public RoleRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Roles.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public Task<Role?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var normalized = code.Trim().ToUpperInvariant();
            return _context.Roles.FirstOrDefaultAsync(r => r.Code == normalized, cancellationToken);
        }

        public Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        }

        public Task<List<Role>> GetAvailableForLeagueAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(r => r.LeagueId == null || r.LeagueId == leagueId)
                .OrderBy(r => r.IsSystem ? 0 : 1)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> NameExistsAsync(string name, Guid? leagueId, Guid? excludeRoleId, CancellationToken cancellationToken = default)
        {
            var normalized = name.Trim();
            return _context.Roles.AnyAsync(
                r => r.Name == normalized
                     && r.LeagueId == leagueId
                     && (excludeRoleId == null || r.Id != excludeRoleId),
                cancellationToken);
        }

        public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            await _context.Roles.AddAsync(role, cancellationToken);
        }

        public void Remove(Role role)
        {
            _context.Roles.Remove(role);
        }
    }
}
