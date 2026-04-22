using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FootballManager.Infrastructure.Repositories;

public sealed class DivisionMatchRulesRepository : IDivisionMatchRulesRepository
{
    private readonly FootballManagerDbContext _context;

    public DivisionMatchRulesRepository(FootballManagerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DivisionMatchRules?> GetByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default)
    {
        return await _context.DivisionMatchRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DivisionSeasonId == divisionSeasonId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<DivisionMatchRules?> GetByDivisionSeasonIdTrackedAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default) =>
        _context.DivisionMatchRules.FirstOrDefaultAsync(x => x.DivisionSeasonId == divisionSeasonId, cancellationToken);

    public async Task AddAsync(DivisionMatchRules entity, CancellationToken cancellationToken = default)
    {
        await _context.DivisionMatchRules.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    public void Update(DivisionMatchRules entity) => _context.DivisionMatchRules.Update(entity);

    public void Remove(DivisionMatchRules entity) => _context.DivisionMatchRules.Remove(entity);
}
