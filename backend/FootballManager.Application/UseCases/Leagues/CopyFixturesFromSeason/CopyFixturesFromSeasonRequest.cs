using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.CopyFixturesFromSeason;

public class CopyFixturesFromSeasonRequest
{
    public Guid LeagueId { get; set; }
    public Guid TargetSeasonId { get; set; }
    public Guid SourceSeasonId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>When set, only copy that division; otherwise all overlapping divisions.</summary>
    public Guid? DivisionId { get; set; }
    /// <summary>When true, home/away are swapped (A vs B becomes B vs A).</summary>
    public bool InvertHomes { get; set; }
}
