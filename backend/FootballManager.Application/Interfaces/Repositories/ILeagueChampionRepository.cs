using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.UseCases.Leagues.LeagueChampions;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ILeagueChampionRepository
    {
        Task<LeagueChampion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<LeagueChampion>> ListAsync(Guid leagueId, LeagueChampionListFilter filter, CancellationToken cancellationToken = default);
        Task<bool> ExistsActiveIdentityAsync(Guid leagueId, string identityKey, Guid? excludeId, CancellationToken cancellationToken = default);
        Task<bool> TeamHasActiveTitleAsync(Guid teamId, CancellationToken cancellationToken = default);
        Task<DivisionSeasonLink?> GetDivisionSeasonLinkAsync(Guid leagueId, Guid seasonId, Guid divisionId, CancellationToken cancellationToken = default);
        Task<bool> DivisionBelongsToLeagueAsync(Guid leagueId, Guid divisionId, CancellationToken cancellationToken = default);
        Task<LeagueChampionCatalog> GetCatalogAsync(Guid leagueId, CancellationToken cancellationToken = default);
        Task AddAsync(LeagueChampion champion, CancellationToken cancellationToken = default);
        Task UnlinkDivisionAsync(Guid divisionId, CancellationToken cancellationToken = default);
    }
}
