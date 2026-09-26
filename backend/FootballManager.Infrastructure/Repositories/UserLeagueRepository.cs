using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Authorization;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class UserLeagueRepository : IUserLeagueRepository
    {
        private readonly FootballManagerDbContext _context;

        public UserLeagueRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> IsUserInLeagueAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.UserLeagues
                .AsNoTracking()
                .AnyAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId, cancellationToken);
        }

        public async Task AddAsync(UserLeague userLeague, CancellationToken cancellationToken = default)
        {
            await _context.UserLeagues.AddAsync(userLeague, cancellationToken);
        }

        public Task<UserLeague?> GetAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
        {
            return _context.UserLeagues
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId, cancellationToken);
        }

        public Task<UserLeague?> GetWithRoleAsync(Guid userId, Guid leagueId, CancellationToken cancellationToken = default)
        {
            return _context.UserLeagues
                .Include(ul => ul.Role)
                    .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(ul => ul.UserId == userId && ul.LeagueId == leagueId, cancellationToken);
        }

        public Task<List<UserLeague>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return _context.UserLeagues
                .Include(ul => ul.User)
                .Include(ul => ul.Role)
                .Where(ul => ul.LeagueId == leagueId)
                .OrderBy(ul => ul.User.FullName)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasPermissionInAnyLeagueAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
        {
            return await _context.UserLeagues
                .AsNoTracking()
                .Where(ul => ul.UserId == userId)
                .AnyAsync(ul =>
                    ul.Role != null && (
                        ul.Role.Code == RoleCodes.Admin
                        || ul.Role.RolePermissions.Any(rp => rp.Permission.Code == permissionCode)),
                    cancellationToken);
        }

        public Task<int> CountByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            return _context.UserLeagues.CountAsync(ul => ul.RoleId == roleId, cancellationToken);
        }

        public void Remove(UserLeague userLeague)
        {
            _context.UserLeagues.Remove(userLeague);
        }
    }
}
