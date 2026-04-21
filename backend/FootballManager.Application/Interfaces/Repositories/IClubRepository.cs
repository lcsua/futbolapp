using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IClubRepository
    {
        Task<Club?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Club?> GetByLeagueAndNameAsync(Guid leagueId, string name, CancellationToken cancellationToken = default);
        Task<List<Club>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Club club, CancellationToken cancellationToken = default);
        void Update(Club club);
    }
}
