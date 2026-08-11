using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class MatchIncidentRepository : IMatchIncidentRepository
    {
        private readonly FootballManagerDbContext _context;

        public MatchIncidentRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<MatchIncident> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.MatchIncidents
                .Include(i => i.Fixture)
                .Include(i => i.Team)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        public async Task<List<MatchIncident>> GetByFixtureIdAsync(Guid fixtureId, CancellationToken cancellationToken = default)
        {
            return await _context.MatchIncidents
                .Include(i => i.Team)
                .Where(i => i.FixtureId == fixtureId)
                .OrderBy(i => i.Minute)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(MatchIncident incident, CancellationToken cancellationToken = default)
        {
            await _context.MatchIncidents.AddAsync(incident, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var incident = await _context.MatchIncidents.FindAsync(new object[] { id }, cancellationToken);
            if (incident != null)
                _context.MatchIncidents.Remove(incident);
        }

        public async Task DeleteByFixtureAndTypeAsync(Guid fixtureId, MatchIncidentType incidentType, CancellationToken cancellationToken = default)
        {
            var incidents = await _context.MatchIncidents
                .Where(i => i.FixtureId == fixtureId && i.IncidentType == incidentType)
                .ToListAsync(cancellationToken);
            if (incidents.Count > 0)
                _context.MatchIncidents.RemoveRange(incidents);
        }

        public async Task<bool> ExistsByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            return await _context.MatchIncidents.AnyAsync(i => i.TeamId == teamId, cancellationToken);
        }
    }
}
