using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Helpers;
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
                .Include(f => f.League)
                .Include(f => f.Season)
                .Include(f => f.DivisionSeason).ThenInclude(ds => ds.Division)
                .Include(f => f.HomeTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.AwayTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.Field)
                .Include(f => f.Result)
                .Include(f => f.Incidents).ThenInclude(i => i.Team)
                .SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        }

        public async Task<Fixture?> FindPublicByTeamSlugsAsync(
            string homeSlug,
            string awayAndSeason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(homeSlug) || string.IsNullOrWhiteSpace(awayAndSeason))
                return null;

            var remainder = awayAndSeason.Trim().ToLowerInvariant();
            var candidates = await _context.Fixtures
                .Include(f => f.League)
                .Include(f => f.Season)
                .Include(f => f.HomeTeamDivisionSeason).ThenInclude(t => t.Team)
                .Include(f => f.AwayTeamDivisionSeason).ThenInclude(t => t.Team)
                .Where(f => f.League != null && f.League.IsPublic)
                .Where(f => f.HomeTeamDivisionSeason.Team.Slug == homeSlug)
                .ToListAsync(cancellationToken);

            Fixture? best = null;
            var bestScore = -1;
            var bestAwayLen = -1;

            foreach (var fixture in candidates)
            {
                var awaySlug = fixture.AwayTeamDivisionSeason?.Team?.Slug;
                if (string.IsNullOrWhiteSpace(awaySlug))
                    continue;

                var seasonSlug = SlugGenerator.Generate(fixture.Season?.Name ?? string.Empty);
                var withSeason = string.IsNullOrEmpty(seasonSlug) ? awaySlug : $"{awaySlug}-{seasonSlug}";
                int score;
                if (remainder.Equals(withSeason, StringComparison.OrdinalIgnoreCase))
                    score = 2;
                else if (remainder.Equals(awaySlug, StringComparison.OrdinalIgnoreCase))
                    score = 1;
                else
                    continue;

                var awayLen = awaySlug.Length;
                var better = score > bestScore
                    || (score == bestScore && awayLen > bestAwayLen)
                    || (score == bestScore && awayLen == bestAwayLen && best != null && ComparePublicMatch(fixture, best) < 0);
                if (better || best == null)
                {
                    best = fixture;
                    bestScore = score;
                    bestAwayLen = awayLen;
                }
            }

            return best;
        }

        private static int ComparePublicMatch(Fixture left, Fixture right)
        {
            var active = (right.Season?.IsActive == true).CompareTo(left.Season?.IsActive == true);
            if (active != 0) return active;
            var date = (right.MatchDate ?? DateOnly.MinValue).CompareTo(left.MatchDate ?? DateOnly.MinValue);
            if (date != 0) return date;
            return (right.StartTime ?? TimeOnly.MinValue).CompareTo(left.StartTime ?? TimeOnly.MinValue);
        }

        public async Task<int> CountBySeasonIdAsync(Guid seasonId, CancellationToken cancellationToken = default)
        {
            return await _context.Fixtures
                .CountAsync(f => f.SeasonId == seasonId, cancellationToken);
        }

        public async Task<int> CountByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default)
        {
            return await _context.Fixtures
                .CountAsync(f => f.DivisionSeasonId == divisionSeasonId, cancellationToken);
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
                .OrderBy(f => f.MatchDate ?? DateOnly.MaxValue)
                .ThenBy(f => f.StartTime ?? TimeOnly.MaxValue)
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
                .ThenBy(f => f.MatchDate ?? DateOnly.MaxValue)
                .ThenBy(f => f.StartTime ?? TimeOnly.MaxValue)
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

        public async Task RemoveByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default)
        {
            var toRemove = await _context.Fixtures
                .Where(f => f.DivisionSeasonId == divisionSeasonId)
                .ToListAsync(cancellationToken);
            _context.Fixtures.RemoveRange(toRemove);
        }

        public async Task RemoveByDivisionIdAsync(Guid divisionId, CancellationToken cancellationToken = default)
        {
            var divisionSeasonIds = await _context.DivisionSeasons
                .Where(ds => ds.DivisionId == divisionId)
                .Select(ds => ds.Id)
                .ToListAsync(cancellationToken);
            var toRemove = await _context.Fixtures
                .Where(f => divisionSeasonIds.Contains(f.DivisionSeasonId))
                .ToListAsync(cancellationToken);
            _context.Fixtures.RemoveRange(toRemove);
        }

        public async Task AddAsync(Fixture fixture, CancellationToken cancellationToken = default)
        {
            await _context.Fixtures.AddAsync(fixture, cancellationToken);
        }

        public void Remove(Fixture fixture)
        {
            _context.Fixtures.Remove(fixture);
        }
    }
}
