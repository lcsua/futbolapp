using System;

namespace FootballManager.Application.UseCases.Leagues.DeleteSeason;

public class DeleteSeasonRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
