using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Player>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<List<Player>> GetByTeamIdsAsync(IEnumerable<Guid> teamIds, CancellationToken cancellationToken = default);
    Task AddAsync(Player player, CancellationToken cancellationToken = default);
    void Update(Player player);
    Task RemoveAsync(Player player, CancellationToken cancellationToken = default);
    Task<bool> HasGoalReferencesAsync(Guid playerId, CancellationToken cancellationToken = default);
}
