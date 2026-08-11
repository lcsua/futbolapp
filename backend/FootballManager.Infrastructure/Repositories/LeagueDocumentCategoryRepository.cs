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
    public class LeagueDocumentCategoryRepository : ILeagueDocumentCategoryRepository
    {
        private readonly FootballManagerDbContext _context;

        public LeagueDocumentCategoryRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<LeagueDocumentCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LeagueDocumentCategories
                .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<LeagueDocumentCategory?> GetByLeagueAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return null;

            var normalized = slug.Trim().ToLowerInvariant();
            return await _context.LeagueDocumentCategories
                .SingleOrDefaultAsync(c => c.LeagueId == leagueId && c.Slug == normalized, cancellationToken);
        }

        public async Task<List<LeagueDocumentCategory>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.LeagueDocumentCategories
                .AsNoTracking()
                .Where(c => c.LeagueId == leagueId)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsByLeagueAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return false;

            var normalized = slug.Trim().ToLowerInvariant();
            return await _context.LeagueDocumentCategories
                .AnyAsync(c => c.LeagueId == leagueId && c.Slug == normalized, cancellationToken);
        }

        public async Task AddAsync(LeagueDocumentCategory category, CancellationToken cancellationToken = default)
        {
            await _context.LeagueDocumentCategories.AddAsync(category, cancellationToken);
        }

        public void Update(LeagueDocumentCategory category)
        {
            _context.LeagueDocumentCategories.Update(category);
        }

        public void Remove(LeagueDocumentCategory category)
        {
            _context.LeagueDocumentCategories.Remove(category);
        }

        public async Task<int> CountDocumentsAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return await _context.LeagueDocuments
                .CountAsync(d => d.CategoryId == categoryId, cancellationToken);
        }
    }
}
