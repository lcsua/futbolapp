using System;

namespace FootballManager.Application.UseCases.Leagues.AssignFixtureDates;

public class AssignFixtureDatesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Date applied to every match in the first round (1-based round order).</summary>
    public DateOnly FirstRoundDate { get; set; }
    /// <summary>When set, only fixtures of that division; otherwise all divisions in the season.</summary>
    public Guid? DivisionId { get; set; }
}
