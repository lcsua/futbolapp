using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Leagues.LeagueChampions;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories
{
    public class LeagueChampionRepository : ILeagueChampionRepository
    {
        private readonly FootballManagerDbContext _context;

        public LeagueChampionRepository(FootballManagerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<LeagueChampion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.LeagueChampions
                .Include(c => c.Team)
                    .ThenInclude(t => t.Club)
                .Include(c => c.Photos)
                .SingleOrDefaultAsync(c => c.Id == id && c.DeletedAt == null, cancellationToken);
        }

        public async Task<List<LeagueChampion>> ListAsync(
            Guid leagueId,
            LeagueChampionListFilter filter,
            CancellationToken cancellationToken = default)
        {
            var query = _context.LeagueChampions
                .AsNoTracking()
                .Include(c => c.Team)
                    .ThenInclude(t => t.Club)
                .Include(c => c.Photos)
                .Where(c => c.LeagueId == leagueId && c.DeletedAt == null);

            if (filter.Year.HasValue)
                query = query.Where(c => c.Year == filter.Year.Value);

            if (filter.TeamId.HasValue)
                query = query.Where(c => c.TeamId == filter.TeamId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Championship))
            {
                var championship = "%" + EscapeLike(filter.Championship.Trim()) + "%";
                query = query.Where(c => EF.Functions.ILike(c.ChampionshipName, championship, "\\"));
            }

            if (!string.IsNullOrWhiteSpace(filter.Division))
            {
                var division = "%" + EscapeLike(filter.Division.Trim()) + "%";
                query = query.Where(c => EF.Functions.ILike(c.DivisionName, division, "\\"));
            }

            return await query
                .OrderByDescending(c => c.Year)
                .ThenByDescending(c => c.WonAt)
                .ThenBy(c => c.ChampionshipName)
                .ThenBy(c => c.DivisionName)
                .ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsActiveIdentityAsync(
            Guid leagueId,
            string identityKey,
            Guid? excludeId,
            CancellationToken cancellationToken = default)
        {
            return _context.LeagueChampions.AnyAsync(
                c => c.LeagueId == leagueId
                    && c.DeletedAt == null
                    && c.IdentityKey == identityKey
                    && (!excludeId.HasValue || c.Id != excludeId.Value),
                cancellationToken);
        }

        public Task<bool> TeamHasActiveTitleAsync(Guid teamId, CancellationToken cancellationToken = default)
        {
            return _context.LeagueChampions.AnyAsync(
                c => c.TeamId == teamId && c.DeletedAt == null,
                cancellationToken);
        }

        public async Task<DivisionSeasonLink?> GetDivisionSeasonLinkAsync(
            Guid leagueId,
            Guid seasonId,
            Guid divisionId,
            CancellationToken cancellationToken = default)
        {
            var divisionSeason = await _context.DivisionSeasons
                .AsNoTracking()
                .Include(ds => ds.Season)
                .Include(ds => ds.Division)
                .Include(ds => ds.TeamAssignments)
                .SingleOrDefaultAsync(
                    ds => ds.SeasonId == seasonId && ds.DivisionId == divisionId && ds.DeletedAt == null,
                    cancellationToken);

            if (divisionSeason?.Season == null || divisionSeason.Division == null)
                return null;
            if (divisionSeason.Season.LeagueId != leagueId || divisionSeason.Season.DeletedAt != null)
                return null;
            if (divisionSeason.Division.LeagueId != leagueId || divisionSeason.Division.DeletedAt != null)
                return null;

            var teamIds = divisionSeason.TeamAssignments
                .Where(a => a.DeletedAt == null)
                .Select(a => a.TeamId)
                .Distinct()
                .ToList();

            return new DivisionSeasonLink(
                divisionSeason.Id,
                divisionSeason.Season.Name,
                divisionSeason.Season.StartDate.Year,
                divisionSeason.Division.Name,
                teamIds);
        }

        public Task<bool> DivisionBelongsToLeagueAsync(Guid leagueId, Guid divisionId, CancellationToken cancellationToken = default)
        {
            return _context.Divisions.AnyAsync(
                d => d.Id == divisionId && d.LeagueId == leagueId && d.DeletedAt == null,
                cancellationToken);
        }

        public async Task<LeagueChampionCatalog> GetCatalogAsync(Guid leagueId, CancellationToken cancellationToken = default)
        {
            var seasons = await _context.Seasons
                .AsNoTracking()
                .Where(s => s.LeagueId == leagueId && s.DeletedAt == null)
                .OrderByDescending(s => s.StartDate)
                .ThenBy(s => s.Name)
                .Select(s => new LeagueChampionSeasonOptionDto(s.Id, s.Name, s.StartDate.Year, s.IsActive))
                .ToListAsync(cancellationToken);

            var divisions = await _context.Divisions
                .AsNoTracking()
                .Where(d => d.LeagueId == leagueId && d.DeletedAt == null)
                .OrderBy(d => d.Name)
                .Select(d => new LeagueChampionDivisionOptionDto(d.Id, d.Name))
                .ToListAsync(cancellationToken);

            var teams = await _context.Teams
                .AsNoTracking()
                .Include(t => t.Club)
                .Where(t => t.LeagueId == leagueId)
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Suffix)
                .ToListAsync(cancellationToken);

            var divisionSeasons = await _context.DivisionSeasons
                .AsNoTracking()
                .Include(ds => ds.Season)
                .Include(ds => ds.TeamAssignments)
                .Where(ds => ds.Season.LeagueId == leagueId && ds.DeletedAt == null && ds.Season.DeletedAt == null)
                .ToListAsync(cancellationToken);

            var teamOptions = teams
                .Select(t => new LeagueChampionTeamOptionDto(
                    t.Id,
                    t.CompetitionName,
                    string.IsNullOrWhiteSpace(t.EffectiveLogoUrl) ? null : t.EffectiveLogoUrl,
                    t.DeletedAt != null,
                    false))
                .ToList();

            var rosters = divisionSeasons
                .Select(ds => new LeagueChampionRosterDto(
                    ds.SeasonId,
                    ds.DivisionId,
                    ds.TeamAssignments.Where(a => a.DeletedAt == null).Select(a => a.TeamId).Distinct().ToList()))
                .ToList();

            return new LeagueChampionCatalog(seasons, divisions, teamOptions, rosters);
        }

        public async Task AddAsync(LeagueChampion champion, CancellationToken cancellationToken = default)
        {
            await _context.LeagueChampions.AddAsync(champion, cancellationToken);
        }

        public async Task UnlinkDivisionAsync(Guid divisionId, CancellationToken cancellationToken = default)
        {
            var champions = await _context.LeagueChampions
                .Where(c => c.DivisionId == divisionId)
                .ToListAsync(cancellationToken);

            foreach (var champion in champions)
                champion.ClearStructuralLink();
        }

        private static string EscapeLike(string value)
            => value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
