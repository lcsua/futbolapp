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
    public class AdvertisementRepository : IAdvertisementRepository
    {
        private readonly FootballManagerDbContext _context;

        public AdvertisementRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Advertisement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Advertisements
                .SingleOrDefaultAsync(a => a.Id == id && a.DeletedAt == null, cancellationToken);
        }

        public async Task<List<Advertisement>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            var advertisements = await _context.Advertisements
                .AsNoTracking()
                .Where(a => a.LeagueId == leagueId && a.DeletedAt == null)
                .ToListAsync(cancellationToken);

            return advertisements
                .OrderBy(a => a.Slot)
                .ThenByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToList();
        }

        public async Task AddAsync(Advertisement advertisement, CancellationToken cancellationToken = default)
        {
            await _context.Advertisements.AddAsync(advertisement, cancellationToken);
        }

        public void Update(Advertisement advertisement)
        {
            _context.Advertisements.Update(advertisement);
        }
    }
}
