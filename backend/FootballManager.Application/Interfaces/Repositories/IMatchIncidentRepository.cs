using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IMatchIncidentRepository
{
    Task<MatchIncident> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<MatchIncident>> GetByFixtureIdAsync(Guid fixtureId, CancellationToken cancellationToken = default);
    Task AddAsync(MatchIncident incident, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteByFixtureAndTypeAsync(Guid fixtureId, MatchIncidentType incidentType, CancellationToken cancellationToken = default);
    Task DeleteByFixtureIdAsync(Guid fixtureId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
}
