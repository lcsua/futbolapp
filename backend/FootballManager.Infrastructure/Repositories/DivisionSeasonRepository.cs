using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class DivisionSeasonRepository : IDivisionSeasonRepository
    {
        private readonly FootballManagerDbContext _context;

        public DivisionSeasonRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<DivisionSeason?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.DivisionSeasons
                .Include(ds => ds.Season)
                .Include(ds => ds.Division)
                .SingleOrDefaultAsync(ds => ds.Id == id, cancellationToken);
        }

        public async Task<DivisionSeason?> GetBySeasonAndDivisionAsync(Guid seasonId, Guid divisionId, CancellationToken cancellationToken = default)
        {
            return await _context.DivisionSeasons
                .SingleOrDefaultAsync(ds => ds.SeasonId == seasonId && ds.DivisionId == divisionId, cancellationToken);
        }

        public async Task AddAsync(DivisionSeason divisionSeason, CancellationToken cancellationToken = default)
        {
            await _context.DivisionSeasons.AddAsync(divisionSeason, cancellationToken);
        }
    }
}
