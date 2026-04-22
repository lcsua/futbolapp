using System;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Per <see cref="DivisionSeason"/> overrides on top of league/season <see cref="MatchRule"/>.
/// Nullable fields inherit from the global match rule row.
/// Field allow-list uses <see cref="DivisionSeasonField"/> only (not JSON).
/// </summary>
public class DivisionMatchRules
{
    public Guid DivisionSeasonId { get; private set; }
    public virtual DivisionSeason DivisionSeason { get; private set; }

    /// <summary>Override <see cref="MatchRule.HalfMinutes"/>.</summary>
    public int? HalfMinutes { get; private set; }

    /// <summary>Override halftime between the two halves (<see cref="MatchRule.BreakMinutes"/>).</summary>
    public int? BreakMinutes { get; private set; }

    public int? WarmupBufferMinutes { get; private set; }
    public int? SlotGranularityMinutes { get; private set; }
    public int? FirstMatchToleranceMinutes { get; private set; }

    /// <summary>Idle minutes between consecutive fixtures on the same field.</summary>
    public int? BreakBetweenMatchesMinutes { get; private set; }

    public string? AllowedTimeRangesJson { get; private set; }

    protected DivisionMatchRules()
    {
    }

    public DivisionMatchRules(
        DivisionSeason divisionSeason,
        int? halfMinutes,
        int? breakMinutes,
        int? warmupBufferMinutes,
        int? slotGranularityMinutes,
        int? firstMatchToleranceMinutes,
        int? breakBetweenMatchesMinutes,
        string? allowedTimeRangesJson)
    {
        DivisionSeason = divisionSeason ?? throw new ArgumentNullException(nameof(divisionSeason));
        DivisionSeasonId = divisionSeason.Id;
        HalfMinutes = halfMinutes;
        BreakMinutes = breakMinutes;
        WarmupBufferMinutes = warmupBufferMinutes;
        SlotGranularityMinutes = slotGranularityMinutes;
        FirstMatchToleranceMinutes = firstMatchToleranceMinutes;
        BreakBetweenMatchesMinutes = breakBetweenMatchesMinutes;
        AllowedTimeRangesJson = allowedTimeRangesJson;
    }

    public void Update(
        int? halfMinutes,
        int? breakMinutes,
        int? warmupBufferMinutes,
        int? slotGranularityMinutes,
        int? firstMatchToleranceMinutes,
        int? breakBetweenMatchesMinutes,
        string? allowedTimeRangesJson)
    {
        HalfMinutes = halfMinutes;
        BreakMinutes = breakMinutes;
        WarmupBufferMinutes = warmupBufferMinutes;
        SlotGranularityMinutes = slotGranularityMinutes;
        FirstMatchToleranceMinutes = firstMatchToleranceMinutes;
        BreakBetweenMatchesMinutes = breakBetweenMatchesMinutes;
        AllowedTimeRangesJson = allowedTimeRangesJson;
    }
}
