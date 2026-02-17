using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories
{
    public interface ICompetitionRuleRepository
    {
        Task<CompetitionRule?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid? seasonId, CancellationToken cancellationToken = default);
        Task AddAsync(CompetitionRule rule, CancellationToken cancellationToken = default);
        void Update(CompetitionRule rule);
        Task RemoveMatchDaysAsync(Guid ruleId, CancellationToken cancellationToken = default);
        Task AddMatchDayAsync(CompetitionMatchDay matchDay, CancellationToken cancellationToken = default);
    }
}
