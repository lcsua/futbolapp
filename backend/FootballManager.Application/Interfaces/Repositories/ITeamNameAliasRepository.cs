using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface ITeamNameAliasRepository
{
    Task<List<TeamNameAlias>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
    Task<TeamNameAlias?> GetByLeagueAndNormalizedAsync(Guid leagueId, string normalizedAlias, CancellationToken cancellationToken = default);
    Task AddAsync(TeamNameAlias alias, CancellationToken cancellationToken = default);
    void Update(TeamNameAlias alias);
}
