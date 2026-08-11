using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchResult;

public sealed class UpdateMatchResultRequest
{
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Status { get; set; }
    public List<MatchGoalAttributionDto>? Goals { get; set; }
}

public sealed class MatchGoalAttributionDto
{
    public Guid TeamId { get; set; }
    public Guid? ScorerPlayerId { get; set; }
    public Guid? AgainstGoalkeeperPlayerId { get; set; }
    public int? Minute { get; set; }
    public string? ScorerName { get; set; }
}
