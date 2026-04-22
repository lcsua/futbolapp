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

public sealed class DivisionSeasonFieldRepository : IDivisionSeasonFieldRepository
{
    private readonly FootballManagerDbContext _context;

    public DivisionSeasonFieldRepository(FootballManagerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<Guid>> GetFieldIdsByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default)
    {
        return await _context.DivisionSeasonFields
            .AsNoTracking()
            .Where(x => x.DivisionSeasonId == divisionSeasonId)
            .Select(x => x.FieldId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ReplaceFieldsAsync(
        DivisionSeason divisionSeason,
        IReadOnlyList<Field> fields,
        CancellationToken cancellationToken = default)
    {
        await _context.DivisionSeasonFields
            .Where(x => x.DivisionSeasonId == divisionSeason.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var field in fields)
        {
            await _context.DivisionSeasonFields.AddAsync(new DivisionSeasonField(divisionSeason, field), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
