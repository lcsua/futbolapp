using System;

namespace FootballManager.Application.UseCases.Matches.GetMatches;

public sealed class GetMatchesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid? DivisionId { get; set; }
    public int? Round { get; set; }
    public Guid UserId { get; set; }
    public bool IsPublic { get; set; } = false;
}
