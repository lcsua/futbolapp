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

public class PlayerRepository : IPlayerRepository
{
    private readonly FootballManagerDbContext _context;

    public PlayerRepository(FootballManagerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Player?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .Include(p => p.Team)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, cancellationToken);
    }

    public async Task<List<Player>> GetByTeamIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _context.Players
            .AsNoTracking()
            .Where(p => p.TeamId == teamId && p.DeletedAt == null)
            .OrderByDescending(p => p.IsActive)
            .ThenBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Player>> GetByTeamIdsAsync(IEnumerable<Guid> teamIds, CancellationToken cancellationToken = default)
    {
        var ids = teamIds?.Distinct().ToList() ?? new List<Guid>();
        if (ids.Count == 0)
            return new List<Player>();

        return await _context.Players
            .AsNoTracking()
            .Where(p => ids.Contains(p.TeamId) && p.DeletedAt == null)
            .OrderBy(p => p.TeamId)
            .ThenByDescending(p => p.IsActive)
            .ThenBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Player player, CancellationToken cancellationToken = default)
    {
        await _context.Players.AddAsync(player, cancellationToken);
    }

    public void Update(Player player)
    {
        _context.Players.Update(player);
    }

    public Task RemoveAsync(Player player, CancellationToken cancellationToken = default)
    {
        _context.Players.Remove(player);
        return Task.CompletedTask;
    }

    public async Task<bool> HasGoalReferencesAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        return await _context.MatchIncidents.AnyAsync(
            i => i.PlayerId == playerId || i.AgainstPlayerId == playerId,
            cancellationToken);
    }
}
