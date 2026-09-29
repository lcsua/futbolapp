using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.ReplanSeasonFixtures;

public sealed class ReplanSeasonFixturesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Zones (divisions) of the same category that are re-planned together.</summary>
    public IReadOnlyList<Guid> DivisionIds { get; set; } = Array.Empty<Guid>();

    /// <summary>First matchday to re-plan; earlier matchdays and matches with a result are kept.</summary>
    public int FromRound { get; set; }

    /// <summary>Pair teams left without a match in a round with a free team of another selected zone.</summary>
    public bool FillByesWithInterzonal { get; set; } = true;
}
