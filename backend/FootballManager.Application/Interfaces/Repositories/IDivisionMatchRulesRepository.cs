using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IDivisionMatchRulesRepository
{
    Task<DivisionMatchRules?> GetByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default);

    Task<DivisionMatchRules?> GetByDivisionSeasonIdTrackedAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default);

    Task AddAsync(DivisionMatchRules entity, CancellationToken cancellationToken = default);

    void Update(DivisionMatchRules entity);

    void Remove(DivisionMatchRules entity);
}
