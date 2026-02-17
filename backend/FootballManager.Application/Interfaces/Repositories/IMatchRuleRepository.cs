using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface IMatchRuleRepository
    {
        Task<MatchRule?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid? seasonId, CancellationToken cancellationToken = default);
        Task AddAsync(MatchRule rule, CancellationToken cancellationToken = default);
        void Update(MatchRule rule);
    }
}
