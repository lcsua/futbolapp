using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IFixtureRepository
{
    Task<int> CountBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
    Task<List<Fixture>> GetBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
    Task RemoveBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
    Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default);
}
