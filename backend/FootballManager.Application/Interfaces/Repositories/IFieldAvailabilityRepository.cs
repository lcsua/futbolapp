using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IFieldAvailabilityRepository
    {
        Task<List<FieldAvailability>> GetByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default);
        Task AddAsync(FieldAvailability availability, CancellationToken cancellationToken = default);
        void Update(FieldAvailability availability);
        void Remove(FieldAvailability availability);
        Task RemoveAllByFieldIdAsync(Guid fieldId, CancellationToken cancellationToken = default);
    }
}
