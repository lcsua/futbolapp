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
    public class DivisionRepository : IDivisionRepository
    {
        private readonly FootballManagerDbContext _context;

        public DivisionRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Division?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Divisions
                .Include(d => d.League)
                .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<List<Division>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Divisions
                .AsNoTracking()
                .Where(d => d.LeagueId == leagueId)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Division division, CancellationToken cancellationToken = default)
        {
            await _context.Divisions.AddAsync(division, cancellationToken);
        }

        public void Update(Division division)
        {
            _context.Divisions.Update(division);
        }
    }
}
