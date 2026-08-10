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
                .Include(t => t.Club)
                .SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<Team?> GetByLeagueIdAndSlugAsync(Guid leagueId, string slug, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(slug)) return null;
            return await _context.Teams
                .Include(t => t.League)
                .Include(t => t.Club)
                .SingleOrDefaultAsync(t => t.LeagueId == leagueId && t.Slug == slug.ToLowerInvariant(), cancellationToken);
        }

        public async Task<List<Team>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            return await _context.Teams
                .AsNoTracking()
                .Include(t => t.Club)
                .Where(t => t.LeagueId == leagueId)
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Team>> GetNeverAssignedByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            var assignedTeamIds = _context.TeamDivisionSeasons.Select(tds => tds.TeamId);
            return await _context.Teams
                .AsNoTracking()
                .Include(t => t.Club)
                .Where(t => t.LeagueId == leagueId && !assignedTeamIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Suffix)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasAnySeasonAssignmentAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            return await _context.TeamDivisionSeasons
                .AnyAsync(tds => tds.TeamId == teamId, cancellationToken);
        }

        public async Task AddAsync(Team team, CancellationToken cancellationToken = default)
        {
            await _context.Teams.AddAsync(team, cancellationToken);
        }

        public void Update(Team team)
        {
            _context.Teams.Update(team);
        }

        public async Task RemoveAsync(Team team, CancellationToken cancellationToken = default)
        {
            var players = await _context.Players.Where(p => p.TeamId == team.Id).ToListAsync(cancellationToken);
            if (players.Count > 0)
                _context.Players.RemoveRange(players);

            _context.Teams.Remove(team);
        }
    }
}
