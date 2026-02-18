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
    public class TeamDivisionSeasonRepository : ITeamDivisionSeasonRepository
    {
        private readonly FootballManagerDbContext _context;

        public TeamDivisionSeasonRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> ExistsAsync(Guid teamId, Guid divisionSeasonId, CancellationToken cancellationToken = default)
        {
            return await _context.TeamDivisionSeasons
                .AnyAsync(tds => tds.TeamId == teamId && tds.DivisionSeasonId == divisionSeasonId, cancellationToken);
        }

        public async Task<List<Guid>> GetTeamIdsAssignedToSeasonAsync(Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.TeamDivisionSeasons
                .Where(tds => tds.DivisionSeason.SeasonId == seasonId)
                .Select(tds => tds.TeamId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(TeamDivisionSeason assignment, CancellationToken cancellationToken = default)
        {
            await _context.TeamDivisionSeasons.AddAsync(assignment, cancellationToken);
        }
    }
}
