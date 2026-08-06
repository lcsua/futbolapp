using System;

namespace FootballManager.Application.UseCases.Leagues.CloseSeason;

public class CloseSeasonRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
