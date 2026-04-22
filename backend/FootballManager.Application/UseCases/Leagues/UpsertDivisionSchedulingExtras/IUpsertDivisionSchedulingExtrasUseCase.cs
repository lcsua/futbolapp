using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.UpsertDivisionSchedulingExtras;

public interface IUpsertDivisionSchedulingExtrasUseCase
{
    Task ExecuteAsync(UpsertDivisionSchedulingExtrasRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Null on a property clears that override (inherit global <see cref="MatchRule"/>).
/// If all division rule fields are null, the division_match_rules row is removed.
/// <see cref="ExplicitFieldIds"/> null = do not change; empty = clear allow-list.
/// </summary>
public sealed class UpsertDivisionSchedulingExtrasRequest
{
    public Guid UserId { get; set; }
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid DivisionId { get; set; }
    public int? HalfMinutes { get; set; }
    public int? BreakMinutes { get; set; }
    public int? WarmupBufferMinutes { get; set; }
    public int? SlotGranularityMinutes { get; set; }
    public int? FirstMatchToleranceMinutes { get; set; }
    public int? BreakBetweenMatchesMinutes { get; set; }
    public string? AllowedTimeRangesJson { get; set; }
    public IReadOnlyList<Guid>? ExplicitFieldIds { get; set; }
}
