using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IDivisionSeasonRepository
    {
        Task<DivisionSeason?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<DivisionSeason?> GetBySeasonAndDivisionAsync(Guid seasonId, Guid divisionId, CancellationToken cancellationToken = default);
        Task<DivisionSeason?> GetBySeasonAndDivisionWithTeamsAsync(Guid seasonId, Guid divisionId, CancellationToken cancellationToken = default);
        Task<List<DivisionSeason>> GetBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
        Task AddAsync(DivisionSeason divisionSeason, CancellationToken cancellationToken = default);
        Task RemoveBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
        Task<List<DivisionSeason>> GetByDivisionIdAsync(Guid divisionId, CancellationToken cancellationToken = default);
        Task RemoveByDivisionIdAsync(Guid divisionId, CancellationToken cancellationToken = default);
    }
}
