using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services;

/// <summary>
/// Division-season overrides → league/season <see cref="MatchRule"/>. Field allow-list: <see cref="DivisionSeasonField"/> only.
/// Cached ~5 minutes per division season (sliding, in-process).
/// </summary>
public class MatchRulesResolver : IMatchRulesResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<Guid, (EffectiveMatchRulesDto Rules, DateTimeOffset Expires)> _cache = new();

    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IMatchRuleRepository _matchRuleRepository;
    private readonly IDivisionMatchRulesRepository _divisionMatchRulesRepository;
    private readonly IDivisionSeasonFieldRepository _divisionSeasonFieldRepository;

    public MatchRulesResolver(
        IDivisionSeasonRepository divisionSeasonRepository,
        IMatchRuleRepository matchRuleRepository,
        IDivisionMatchRulesRepository divisionMatchRulesRepository,
        IDivisionSeasonFieldRepository divisionSeasonFieldRepository)
    {
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _matchRuleRepository = matchRuleRepository ?? throw new ArgumentNullException(nameof(matchRuleRepository));
        _divisionMatchRulesRepository = divisionMatchRulesRepository ?? throw new ArgumentNullException(nameof(divisionMatchRulesRepository));
        _divisionSeasonFieldRepository = divisionSeasonFieldRepository ?? throw new ArgumentNullException(nameof(divisionSeasonFieldRepository));
    }

    public async Task<EffectiveMatchRulesDto> GetEffectiveRulesAsync(Guid divisionSeasonId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        if (_cache.TryGetValue(divisionSeasonId, out var entry) && entry.Expires > now)
            return entry.Rules;

        var resolved = await ResolveUncachedAsync(divisionSeasonId, cancellationToken).ConfigureAwait(false);
        _cache[divisionSeasonId] = (resolved, now.Add(CacheDuration));
        return resolved;
    }

    private async Task<EffectiveMatchRulesDto> ResolveUncachedAsync(Guid divisionSeasonId, CancellationToken cancellationToken)
    {
        var ds = await _divisionSeasonRepository.GetByIdAsync(divisionSeasonId, cancellationToken).ConfigureAwait(false);
        if (ds?.Season == null)
            throw new KeyNotFoundException($"DivisionSeason {divisionSeasonId} not found.");

        var leagueId = ds.Season.LeagueId;
        var seasonId = ds.SeasonId;

        var matchRule = await _matchRuleRepository.GetByLeagueAndSeasonAsync(leagueId, seasonId, cancellationToken).ConfigureAwait(false)
                        ?? await _matchRuleRepository.GetByLeagueAndSeasonAsync(leagueId, null, cancellationToken).ConfigureAwait(false);
        if (matchRule == null)
            throw new InvalidOperationException("Match rules must exist for the league or season before resolving effective rules.");

        var divisionExtra = await _divisionMatchRulesRepository.GetByDivisionSeasonIdAsync(divisionSeasonId, cancellationToken).ConfigureAwait(false);
        var explicitFieldIds = await _divisionSeasonFieldRepository.GetFieldIdsByDivisionSeasonIdAsync(divisionSeasonId, cancellationToken).ConfigureAwait(false);

        var half = divisionExtra?.HalfMinutes ?? matchRule.HalfMinutes;
        var breakHalves = divisionExtra?.BreakMinutes ?? matchRule.BreakMinutes;
        var warm = divisionExtra?.WarmupBufferMinutes ?? matchRule.WarmupBufferMinutes;
        var totalBlock = (half * 2) + breakHalves + warm;
        if (totalBlock < 1)
            totalBlock = 1;

        var slotGranularity = divisionExtra?.SlotGranularityMinutes ?? matchRule.SlotGranularityMinutes;
        if (slotGranularity < 1)
            slotGranularity = 1;

        var firstTol = divisionExtra?.FirstMatchToleranceMinutes ?? matchRule.FirstMatchToleranceMinutes;
        if (firstTol < 0)
            firstTol = 0;

        var breakBetween = divisionExtra?.BreakBetweenMatchesMinutes ?? 0;
        if (breakBetween < 0)
            breakBetween = 0;

        IReadOnlyList<Guid>? allowedFields = null;
        if (explicitFieldIds.Count > 0)
            allowedFields = explicitFieldIds;

        var rangeTuples = SchedulingRulesJsonParser.TryParseKickoffRanges(divisionExtra?.AllowedTimeRangesJson);

        IReadOnlyList<EffectiveKickoffTimeRangeDto>? ranges = rangeTuples == null
            ? null
            : rangeTuples.Select(r => new EffectiveKickoffTimeRangeDto(r.Item1, r.Item2)).ToList();

        return new EffectiveMatchRulesDto
        {
            TotalMatchSlotBlockMinutes = totalBlock,
            SlotGranularityMinutes = slotGranularity,
            FirstMatchToleranceMinutes = firstTol,
            BreakBetweenMatchesMinutes = breakBetween,
            AllowedFieldIds = allowedFields,
            AllowedKickoffTimeRanges = ranges,
        };
    }

    public void InvalidateCache() => _cache.Clear();

    public void InvalidateCache(Guid divisionSeasonId) => _cache.TryRemove(divisionSeasonId, out _);
}
