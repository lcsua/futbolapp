using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ITeamDivisionSeasonRepository
    {
        Task<bool> ExistsAsync(Guid teamId, Guid divisionSeasonId, CancellationToken cancellationToken = default);
        Task<List<Guid>> GetTeamIdsAssignedToSeasonAsync(Guid seasonId, CancellationToken cancellationToken = default);
        Task AddAsync(TeamDivisionSeason assignment, CancellationToken cancellationToken = default);
        Task RemoveBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default);
    }
}
