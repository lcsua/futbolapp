using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Infrastructure.Persistence;

namespace FootballManager.Infrastructure.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FootballManagerDbContext _context;

        public UnitOfWork(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
