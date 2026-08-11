using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories;

public class TeamNameAliasRepository : ITeamNameAliasRepository
{
    private readonly FootballManagerDbContext _context;

    public TeamNameAliasRepository(FootballManagerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<TeamNameAlias>> GetByLeagueIdAsync(Guid leagueId, CancellationToken cancellationToken = default)
    {
        return await _context.TeamNameAliases
            .AsNoTracking()
            .Where(a => a.LeagueId == leagueId && a.DeletedAt == null)
            .OrderBy(a => a.Alias)
            .ToListAsync(cancellationToken);
    }

    public async Task<TeamNameAlias?> GetByLeagueAndNormalizedAsync(
        Guid leagueId,
        string normalizedAlias,
        CancellationToken cancellationToken = default)
    {
        return await _context.TeamNameAliases
            .FirstOrDefaultAsync(
                a => a.LeagueId == leagueId && a.NormalizedAlias == normalizedAlias && a.DeletedAt == null,
                cancellationToken);
    }

    public async Task AddAsync(TeamNameAlias alias, CancellationToken cancellationToken = default)
    {
        await _context.TeamNameAliases.AddAsync(alias, cancellationToken);
    }

    public void Update(TeamNameAlias alias)
    {
        _context.TeamNameAliases.Update(alias);
    }
}
