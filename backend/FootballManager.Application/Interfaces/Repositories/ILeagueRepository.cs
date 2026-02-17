using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ILeagueRepository
    {
        Task<League?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<League>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<League>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default); // returning List for simplicity as per requirements, could be paged
        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
        Task AddAsync(League league, CancellationToken cancellationToken = default);
        void Update(League league);
        void Delete(League league);
    }
}
