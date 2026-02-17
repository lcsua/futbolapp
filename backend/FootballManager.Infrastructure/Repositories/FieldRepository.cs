using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class FieldRepository : IFieldRepository
    {
        private readonly FootballManagerDbContext _context;

        public FieldRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Field?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Fields
                .Include(f => f.League)
                .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<List<Field>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Fields
                .AsNoTracking()
                .Where(f => f.LeagueId == leagueId)
                .OrderBy(f => f.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Field field, CancellationToken cancellationToken = default)
        {
            await _context.Fields.AddAsync(field, cancellationToken);
        }

        public void Update(Field field)
        {
            _context.Fields.Update(field);
        }
    }
}
