using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.Interfaces;

/// <summary>
/// Resolves effective scheduling rules for a division season (division overrides → league extras → legacy <see cref="FootballManager.Domain.Entities.MatchRule"/>).
/// </summary>
public interface IMatchRulesResolver
{
    Task<EffectiveMatchRulesDto> GetEffectiveRulesAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default);

    /// <summary>Clears all cached effective rules (e.g. after league-level scheduling extras change).</summary>
    void InvalidateCache();

    /// <summary>Removes cached rules for one division season (e.g. after division-level change).</summary>
    void InvalidateCache(Guid divisionSeasonId);
}
