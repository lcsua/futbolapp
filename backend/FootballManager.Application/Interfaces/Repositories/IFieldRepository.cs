using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IFieldRepository
    {
        Task<Field?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Field>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Field field, CancellationToken cancellationToken = default);
        void Update(Field field);
    }
}
