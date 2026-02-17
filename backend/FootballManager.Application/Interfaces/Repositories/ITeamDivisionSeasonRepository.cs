using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ITeamDivisionSeasonRepository
    {
        Task<bool> ExistsAsync(Guid teamId, Guid divisionSeasonId, CancellationToken cancellationToken = default);
        Task AddAsync(TeamDivisionSeason assignment, CancellationToken cancellationToken = default);
    }
}
