using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class MatchRuleRepository : IMatchRuleRepository
    {
        private readonly FootballManagerDbContext _context;

        public MatchRuleRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<MatchRule?> GetByLeagueAndSeasonAsync(Guid leagueId, Guid? seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.MatchRules
                .SingleOrDefaultAsync(m => m.LeagueId == leagueId && m.SeasonId == seasonId, cancellationToken);
        }

        public async Task AddAsync(MatchRule rule, CancellationToken cancellationToken = default)
        {
            await _context.MatchRules.AddAsync(rule, cancellationToken);
        }

        public void Update(MatchRule rule)
        {
            _context.MatchRules.Update(rule);
        }
    }
}
