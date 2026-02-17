using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IFieldBlackoutRepository
    {
        Task<FieldBlackout?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<FieldBlackout>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default);
        Task AddAsync(FieldBlackout blackout, CancellationToken cancellationToken = default);
        void Remove(FieldBlackout blackout);
    }
}
