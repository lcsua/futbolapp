using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IAdvertisementRepository
    {
        Task<Advertisement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Advertisement>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(Advertisement advertisement, CancellationToken cancellationToken = default);
        void Update(Advertisement advertisement);
    }
}
