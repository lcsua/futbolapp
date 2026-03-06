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
    public class FixtureRepository : IFixtureRepository
    {
        private readonly FootballManagerDbContext _context;

        public FixtureRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Fixture> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Fixtures
                .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
                .Include(f => f.HomeTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.AwayTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.Field)
                .Include(f => f.Result)
                .Include(f => f.Incidents).ThenInclude(i => i.Team)
                .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<int> CountBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.Fixtures
                .CountAsync(f => f.SeasonId == seasonId, cancellationToken);
        }

        public async Task<List<Fixture>> GetBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.Fixtures
                .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
                .Include(f => f.HomeTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.AwayTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.Field)
                .Include(f => f.Result)
                .Where(f => f.SeasonId == seasonId)
                .OrderBy(f => f.MatchDate)
                .ThenBy(f => f.StartTime)
                .ThenBy(f => f.DivisionSeason.Division.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Fixture>> GetBySeasonAndDivisionAndRoundAsync(Guid seasonId, Guid? divisionSeasonId, int? round, CancellationToken cancellationToken = default)
        {
            var query = _context.Fixtures
                .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
                .Include(f => f.HomeTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.AwayTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.Field)
                .Include(f => f.Result)
                .Where(f => f.SeasonId == seasonId);

            if (divisionSeasonId.HasValue)
                query = query.Where(f => f.DivisionSeasonId == divisionSeasonId.Value);
            if (round.HasValue)
                query = query.Where(f => f.RoundNumber == round.Value);

            return await query
                .OrderBy(f => f.RoundNumber)
                .ThenBy(f => f.MatchDate)
                .ThenBy(f => f.StartTime)
                .ThenBy(f => f.DivisionSeason.Division.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task RemoveBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default)
        {
            var toRemove = await _context.Fixtures
                .Where(f => f.SeasonId == seasonId)
                .ToListAsync(cancellationToken);
            _context.Fixtures.RemoveRange(toRemove);
        }

        public async Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default)
        {
            await _context.Fixtures.AddAsync(fixture, cancellationToken);
        }
    }
}
