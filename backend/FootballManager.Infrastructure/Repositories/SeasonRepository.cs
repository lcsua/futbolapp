using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class SeasonRepository : ISeasonRepository
    {
        private readonly FootballManagerDbContext _context;

        public SeasonRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Season?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Seasons
                .Include(s => s.DivisionSeasons)
                .ThenInclude(ds => ds.Division)
                .Include(s => s.DivisionSeasons)
                .ThenInclude(ds => ds.TeamAssignments)
                .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        }

        public async Task<List<Season>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Seasons
                .AsNoTracking()
                .Where(s => s.LeagueId == leagueId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Season season, CancellationToken cancellationToken = default)
        {
            await _context.Seasons.AddAsync(season, cancellationToken);
        }

        public void Update(Season season)
        {
            _context.Seasons.Update(season);
        }
    }
}
