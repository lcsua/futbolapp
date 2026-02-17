using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IDivisionRepository
    {
        Task<Division?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Division>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Division division, CancellationToken cancellationToken = default);
        void Update(Division division);
    }
}
