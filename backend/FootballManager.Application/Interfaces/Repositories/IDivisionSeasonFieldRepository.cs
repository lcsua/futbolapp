using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Interfaces.Repositories;

public interface IDivisionSeasonFieldRepository
{
    Task<IReadOnlyList<Guid>> GetFieldIdsByDivisionSeasonIdAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default);

    Task ReplaceFieldsAsync(
        DivisionSeason divisionSeason,
        IReadOnlyList<Field> fields,
        CancellationToken cancellationToken = default);
}
