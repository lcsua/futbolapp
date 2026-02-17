using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ISeasonRepository
    {
        Task<Season?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Season>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Season season, CancellationToken cancellationToken = default);
        void Update(Season season);
    }
}
