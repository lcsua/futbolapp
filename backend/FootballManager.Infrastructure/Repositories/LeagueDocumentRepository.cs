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
    public class LeagueDocumentRepository : ILeagueDocumentRepository
    {
        private readonly FootballManagerDbContext _context;

        public LeagueDocumentRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<LeagueDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LeagueDocuments
                .SingleOrDefaultAsync(d => d.Id == id, cancellationToken);
        }

        public async Task<List<LeagueDocument>> GetByLeagueIdAsync(Guid leagueId, Guid? categoryId = null, CancellationToken cancellationToken = default)
        {
            var query = _context.LeagueDocuments
                .AsNoTracking()
                .Where(d => d.LeagueId == leagueId);

            if (categoryId.HasValue)
                query = query.Where(d => d.CategoryId == categoryId.Value);

            return await query
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Title)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(LeagueDocument document, CancellationToken cancellationToken = default)
        {
            await _context.LeagueDocuments.AddAsync(document, cancellationToken);
        }

        public void Update(LeagueDocument document)
        {
            _context.LeagueDocuments.Update(document);
        }

        public void Remove(LeagueDocument document)
        {
            _context.LeagueDocuments.Remove(document);
        }
    }
}
