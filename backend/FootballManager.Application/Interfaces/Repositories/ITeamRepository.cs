using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ITeamRepository
    {
        Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Team>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Team team, CancellationToken cancellationToken = default);
        void Update(Team team);
    }
}
