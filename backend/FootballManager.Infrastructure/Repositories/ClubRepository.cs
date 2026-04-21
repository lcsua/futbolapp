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
    public class ClubRepository : IClubRepository
    {
        private readonly FootballManagerDbContext _context;

        public ClubRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Club?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Clubs.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Club?> GetByLeagueAndNameAsync(Guid leagueId, string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var normalized = name.Trim();
            return await _context.Clubs
                .SingleOrDefaultAsync(c => c.LeagueId == leagueId && c.Name == normalized, cancellationToken);
        }

        public async Task<List<Club>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Clubs
                .AsNoTracking()
                .Where(c => c.LeagueId == leagueId)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Club club, CancellationToken cancellationToken = default)
        {
            await _context.Clubs.AddAsync(club, cancellationToken);
        }

        public void Update(Club club)
        {
            _context.Clubs.Update(club);
        }
    }
}
