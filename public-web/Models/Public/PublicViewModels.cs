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
    public string? FieldName { get; set; }
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

public class TeamMatchListPartialViewModel
{
    public List<MatchViewModel> Matches { get; set; } = new();
    public string Mode { get; set; } = "next"; // next | results
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string EmptyText { get; set; } = string.Empty;
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
    public int? DefaultRound { get; set; }
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

public class LeagueDocumentsViewModel
{
    public List<LeagueDocumentCategoryViewModel> Categories { get; set; } = new();
}

public class LeagueDocumentCategoryViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool RequiresDocumentDate { get; set; }
    public int SortOrder { get; set; }
    public List<LeagueDocumentItemViewModel> Documents { get; set; } = new();
}

public class LeagueDocumentItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public DateOnly? DocumentDate { get; set; }
    public bool IsImage { get; set; }
    public int SortOrder { get; set; }
}

public class LeagueDocumentsPageViewModel
{
    public LeagueViewModel League { get; set; } = new();
    public LeagueDocumentsViewModel Documents { get; set; } = new();
    public LeagueDocumentCategoryViewModel? ActiveCategory { get; set; }
}

/// <summary>V2 league home composition (portada). Data sliced from existing public endpoints.</summary>
public class LeagueHomeViewModel
{
    public LeagueViewModel League { get; set; } = new();
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public List<DivisionViewModel> Divisions { get; set; } = new();
    public List<LeagueHomeDivisionLeaderViewModel> DivisionLeaders { get; set; } = new();
    public LeagueHomeNextFechaViewModel? NextFecha { get; set; }
}

public class LeagueHomeNextFechaViewModel
{
    public int Round { get; set; }
    public DateTime? DisplayDate { get; set; }
    public int MatchCount { get; set; }
}

public class LeagueHomeDivisionLeaderViewModel
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public TeamViewModel Team { get; set; } = new();
    public int Points { get; set; }
}

public class StandingsPreviewGroupViewModel
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public List<StandingsRowViewModel> Rows { get; set; } = new();
}

