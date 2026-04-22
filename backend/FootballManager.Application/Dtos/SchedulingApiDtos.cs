using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FootballManager.Application.Dtos;

public static class SchedulingDtoMapper
{
    public static EffectiveMatchRulesResponse ToResponse(EffectiveMatchRulesDto dto)
    {
        return new EffectiveMatchRulesResponse
        {
            TotalMatchSlotBlockMinutes = dto.TotalMatchSlotBlockMinutes,
            SlotGranularityMinutes = dto.SlotGranularityMinutes,
            FirstMatchToleranceMinutes = dto.FirstMatchToleranceMinutes,
            BreakBetweenMatchesMinutes = dto.BreakBetweenMatchesMinutes,
            AllowedFieldIds = dto.AllowedFieldIds,
            AllowedKickoffTimeRanges = dto.AllowedKickoffTimeRanges?.Select(r => new EffectiveKickoffTimeRangeResponse
            {
                Start = r.Start.ToString("HH:mm", CultureInfo.InvariantCulture),
                End = r.End.ToString("HH:mm", CultureInfo.InvariantCulture),
            }).ToList(),
        };
    }
}

public sealed class EffectiveKickoffTimeRangeResponse
{
    public string Start { get; init; } = "";
    public string End { get; init; } = "";
}

public sealed class EffectiveMatchRulesResponse
{
    public int TotalMatchSlotBlockMinutes { get; init; }
    public int SlotGranularityMinutes { get; init; }
    public int FirstMatchToleranceMinutes { get; init; }
    public int BreakBetweenMatchesMinutes { get; init; }
    public IReadOnlyList<Guid>? AllowedFieldIds { get; init; }
    public IReadOnlyList<EffectiveKickoffTimeRangeResponse>? AllowedKickoffTimeRanges { get; init; }
}

public sealed class MatchRuleSchedulingSummaryDto
{
    public Guid Id { get; init; }
    public Guid LeagueId { get; init; }
    public Guid? SeasonId { get; init; }
    public int HalfMinutes { get; init; }
    public int BreakMinutes { get; init; }
    public int WarmupBufferMinutes { get; init; }
    public int DerivedTotalMatchSlotMinutes { get; init; }
    public int SlotGranularityMinutes { get; init; }
    public int FirstMatchToleranceMinutes { get; init; }
}

/// <summary>Nullable fields: null means inherit from <see cref="MatchRuleSchedulingSummaryDto"/> (global).</summary>
public sealed class DivisionMatchRulesExtrasDto
{
    public Guid DivisionSeasonId { get; init; }
    public int? HalfMinutes { get; init; }
    public int? BreakMinutes { get; init; }
    public int? WarmupBufferMinutes { get; init; }
    public int? SlotGranularityMinutes { get; init; }
    public int? FirstMatchToleranceMinutes { get; init; }
    public int? BreakBetweenMatchesMinutes { get; init; }
    public string? AllowedTimeRangesJson { get; init; }
}

/// <summary>Effective rules plus global match rule and division overrides for admin UI.</summary>
public sealed class SchedulingEffectiveDetailResponse
{
    public Guid DivisionSeasonId { get; init; }
    public EffectiveMatchRulesResponse Effective { get; init; } = null!;
    public MatchRuleSchedulingSummaryDto GlobalMatchRule { get; init; } = null!;
    public DivisionMatchRulesExtrasDto? DivisionExtras { get; init; }
    public IReadOnlyList<Guid> ExplicitFieldIds { get; init; } = Array.Empty<Guid>();
}

public sealed class DivisionSchedulingExtrasBundleResponse
{
    public Guid DivisionSeasonId { get; init; }
    public MatchRuleSchedulingSummaryDto GlobalMatchRule { get; init; } = null!;
    public DivisionMatchRulesExtrasDto? DivisionExtras { get; init; }
    public IReadOnlyList<Guid> ExplicitFieldIds { get; init; } = Array.Empty<Guid>();
    public EffectiveMatchRulesResponse EffectivePreview { get; init; } = null!;
}
