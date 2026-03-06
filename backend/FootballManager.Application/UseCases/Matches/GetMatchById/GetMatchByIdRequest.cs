using System;

namespace FootballManager.Application.UseCases.Matches.GetMatchById;

public sealed class GetMatchByIdRequest
{
    public Guid LeagueId { get; set; }
    public Guid MatchId { get; set; }
    public Guid UserId { get; set; }
}
