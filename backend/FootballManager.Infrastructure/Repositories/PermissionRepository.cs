using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly FootballManagerDbContext _context;

        public PermissionRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        public Task<List<Permission>> GetByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var set = codes.Select(c => c.Trim()).Where(c => c.Length > 0).Distinct().ToList();
            return _context.Permissions
                .Where(p => set.Contains(p.Code))
                .ToListAsync(cancellationToken);
        }
    }
}
