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

public class SeasonPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
}

public class DivisionPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class StandingSummaryDto
{
    public int Position { get; set; }
    public int Played { get; set; }
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
}

public class TeamSummaryPublicDto
{
    public TeamPublicDto Team { get; set; } = new();
    public List<SeasonPublicDto> ActiveSeasons { get; set; } = new();
    public List<MatchPublicDto> NextMatches { get; set; } = new();
    public List<MatchPublicDto> LastResults { get; set; } = new();
    public StandingSummaryDto? Standing { get; set; }
}

public class DivisionSummaryPublicDto
{
    public DivisionPublicDto Division { get; set; } = new();
    public List<StandingsRowPublicDto> Standings { get; set; } = new();
}

public class DivisionGroupDto<T>
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
}

public class SeasonGroupedDto<T>
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public List<DivisionGroupDto<T>> Divisions { get; set; } = new();
}
