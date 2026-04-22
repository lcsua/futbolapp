using System;
using System.Collections.Generic;

namespace FootballManager.Application.Dtos;

/// <summary>
/// Resolved scheduling parameters for a <see cref="FootballManager.Domain.Entities.DivisionSeason"/> (division rules override league rules).
/// </summary>
public sealed class EffectiveMatchRulesDto
{
    /// <summary>Total minutes occupied on the field per match (halves + half-time + warmup buffer).</summary>
    public int TotalMatchSlotBlockMinutes { get; init; }

    public int SlotGranularityMinutes { get; init; }

    public int FirstMatchToleranceMinutes { get; init; }

    /// <summary>Extra idle minutes after each match on the same field before the next kickoff can be scheduled.</summary>
    public int BreakBetweenMatchesMinutes { get; init; }

    /// <summary>When non-null, only these field ids may be used. When null, any available league field may be used.</summary>
    public IReadOnlyList<Guid>? AllowedFieldIds { get; init; }

/// <summary>
/// When non-null, kickoff time must fall in [Start, End) for at least one segment (local time on match day).
/// </summary>
    public IReadOnlyList<EffectiveKickoffTimeRangeDto>? AllowedKickoffTimeRanges { get; init; }
}

public sealed record EffectiveKickoffTimeRangeDto(TimeOnly Start, TimeOnly End);
