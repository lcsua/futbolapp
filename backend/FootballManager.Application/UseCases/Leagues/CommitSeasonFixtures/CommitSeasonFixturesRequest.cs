using System;

namespace FootballManager.Application.UseCases.Leagues.CommitSeasonFixtures;

public sealed class CommitSeasonFixturesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
