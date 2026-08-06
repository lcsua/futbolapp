using System;

namespace FootballManager.Application.UseCases.Leagues.ReopenSeason;

public class ReopenSeasonRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
