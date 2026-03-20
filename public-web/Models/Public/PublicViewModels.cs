namespace PublicWeb.Models.Public;

public class LeagueViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public class TeamViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public int? FoundedYear { get; set; }
    public string? PhotoUrl { get; set; }
}

public class StandingsRowViewModel
{
    public int Position { get; set; }
    public TeamViewModel Team { get; set; } = new();
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
    public int Points { get; set; }
}

public class MatchViewModel
{
    public Guid Id { get; set; }
    public DateTime Kickoff { get; set; }
    public string Status { get; set; } = "Scheduled"; // Scheduled, InPlay, Finished
    public TeamViewModel HomeTeam { get; set; } = new();
    public TeamViewModel AwayTeam { get; set; } = new();
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string? MatchDay { get; set; }
}

public class DivisionViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class SeasonViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<DivisionViewModel> Divisions { get; set; } = new();
}

public class DivisionGroupViewModel<T>
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public List<T> Data { get; set; } = new();
}

public class SeasonGroupedViewModel<T>
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public List<DivisionGroupViewModel<T>> Divisions { get; set; } = new();
}

public class MatchdayGroupViewModel
{
    public int Round { get; set; }
    public List<MatchViewModel> Matches { get; set; } = new();
}

