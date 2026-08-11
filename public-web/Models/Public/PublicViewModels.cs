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
    public string? LeagueSlug { get; set; }
}

public class StandingSummaryViewModel
{
    public int Position { get; set; }
    public int Played { get; set; }
    public int Points { get; set; }
    public int Wins { get; set; }
    public int Draws { get; set; }
    public int Losses { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public string? DivisionName { get; set; }
    public int GoalDifference => GoalsFor - GoalsAgainst;
}

public class TeamDetailViewModel
{
    public TeamViewModel Team { get; set; } = new();
    public LeagueViewModel? League { get; set; }
    public SeasonViewModel? Season { get; set; }
    public StandingSummaryViewModel? Standing { get; set; }
    public List<MatchViewModel> NextMatches { get; set; } = new();
    public List<MatchViewModel> LastResults { get; set; } = new();
    public int PageSize { get; set; } = 5;
    public int NextMatchesPage { get; set; } = 1;
    public int NextMatchesTotal { get; set; }
    public int LastResultsPage { get; set; } = 1;
    public int LastResultsTotal { get; set; }

    public int NextMatchesTotalPages => Math.Max(1, (int)Math.Ceiling(NextMatchesTotal / (double)Math.Max(1, PageSize)));
    public int LastResultsTotalPages => Math.Max(1, (int)Math.Ceiling(LastResultsTotal / (double)Math.Max(1, PageSize)));
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

