using System;

namespace FootballManager.Application.UseCases.Leagues.GenerateSeasonFixtures;

public sealed class GenerateSeasonFixturesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid? DivisionId { get; set; }
    public Guid UserId { get; set; }
}
