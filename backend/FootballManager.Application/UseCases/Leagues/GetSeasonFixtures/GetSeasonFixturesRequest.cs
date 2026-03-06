using System;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonFixtures;

public sealed class GetSeasonFixturesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
