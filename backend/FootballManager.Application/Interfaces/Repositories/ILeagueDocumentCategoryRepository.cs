using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ILeagueDocumentCategoryRepository
    {
        Task<LeagueDocumentCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<LeagueDocumentCategory?> GetByLeagueAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default);
        Task<List<LeagueDocumentCategory>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByLeagueAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default);
        Task AddAsync(LeagueDocumentCategory category, CancellationToken cancellationToken = default);
        void Update(LeagueDocumentCategory category);
        void Remove(LeagueDocumentCategory category);
        Task<int> CountDocumentsAsync(Guid categoryId, CancellationToken cancellationToken = default);
    }
}
