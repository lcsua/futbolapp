using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ILeagueDocumentRepository
    {
        Task<LeagueDocument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<LeagueDocument>> GetByLeagueIdAsync(Guid leagueId, Guid? categoryId = null, CancellationToken cancellationToken = default);
        Task AddAsync(LeagueDocument document, CancellationToken cancellationToken = default);
        void Update(LeagueDocument document);
        void Remove(LeagueDocument document);
    }
}
