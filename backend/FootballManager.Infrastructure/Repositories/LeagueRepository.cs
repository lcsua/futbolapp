using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class LeagueRepository : ILeagueRepository
    {
        private readonly FootballManagerDbContext _context;

        public LeagueRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<League?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Leagues
                .Include(l => l.Seasons)
                .ThenInclude(s => s.DivisionSeasons)
                .SingleOrDefaultAsync(l => l.Id == id, cancellationToken);
        }

        public async Task<List<League>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Leagues
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<League>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.UserLeagues
                .AsNoTracking()
                .Where(ul => ul.UserId == userId)
                .Include(ul => ul.League)
                .Select(ul => ul.League)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await _context.Leagues
                .AnyAsync(l => l.Name == name, cancellationToken);
        }

        public async Task AddAsync(League league, CancellationToken cancellationToken = default)
        {
            await _context.Leagues.AddAsync(league, cancellationToken);
        }

        public void Update(League league)
        {
            _context.Leagues.Update(league);
        }

        public void Delete(League league)
        {
            _context.Leagues.Remove(league);
        }
    }
}
