using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class TeamRepository : ITeamRepository
    {
        private readonly FootballManagerDbContext _context;

        public TeamRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Teams
                .Include(t => t.Players)
                .Include(t => t.League)
                .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<List<Team>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Teams
                .AsNoTracking()
                .Where(t => t.LeagueId == leagueId)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
        {
            await _context.Teams.AddAsync(team, cancellationToken);
        }

        public void Update(Team team)
        {
            _context.Teams.Update(team);
        }
    }
}
