using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class CompetitionRuleRepository : ICompetitionRuleRepository
    {
        private readonly FootballManagerDbContext _context;

        public CompetitionRuleRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<CompetitionRule?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid? seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.CompetitionRules
                .Include(c => c.MatchDays)
                .SingleOrDefaultAsync(c => c.LeagueId == leagueId && c.SeasonId == seasonId, cancellationToken);
        }

        public async Task AddAsync(CompetitionRule rule, CancellationToken cancellationToken = default)
        {
            await _context.CompetitionRules.AddAsync(rule, cancellationToken);
        }

        public void Update(CompetitionRule rule)
        {
            _context.CompetitionRules.Update(rule);
        }

        public async Task RemoveMatchDaysAsync(Guid ruleId, CancellationToken cancellationToken = default)
        {
            var toRemove = await _context.CompetitionMatchDays.Where(m => m.CompetitionRuleId == ruleId).ToListAsync(cancellationToken);
            _context.CompetitionMatchDays.RemoveRange(toRemove);
        }

        public async Task AddMatchDayAsync(CompetitionMatchDay matchDay, CancellationToken cancellationToken = default)
        {
            await _context.CompetitionMatchDays.AddAsync(matchDay, cancellationToken);
        }
    }
}
