using System;

namespace FootballManager.Application.UseCases.Leagues.ShiftFixtureDates;

public class ShiftFixtureDatesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Inclusive cutoff. Every pending match on or after this date moves forward one week, in every division.</summary>
    public DateOnly FromDate { get; set; }
}
