using System;
using System.Collections.Generic;

namespace FootballManager.Api.Models.Public;

public class LeaguePublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TeamPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LogoUrl { get; set; }
}

public class StandingsRowPublicDto
{
    public int Position { get; set; }
    public TeamPublicDto Team { get; set; } = new TeamPublicDto();
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Points { get; set; }
}

public class MatchPublicDto
{
    public Guid Id { get; set; }
    public DateTime Kickoff { get; set; }
    public string Status { get; set; } = string.Empty;
    public TeamPublicDto HomeTeam { get; set; } = new();
    public TeamPublicDto AwayTeam { get; set; } = new();
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}
