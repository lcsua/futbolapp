namespace PublicWeb.Models.Public;

public sealed class LeaguesIndexPageViewModel
{
    public List<LeagueViewModel> AmateurLeagues { get; set; } = new();
    public List<ProfessionalCompetitionCardViewModel> ArgentineTournaments { get; set; } = new();
    public bool ArgentineTournamentsUnavailable { get; set; }
}

public sealed class ProfessionalCompetitionCardViewModel
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public string? LogoUrl { get; set; }
    public int Season { get; set; }
    public string CurrentTournament { get; set; } = "";
}

public sealed class ProfessionalCompetitionDetailViewModel
{
    public ProfessionalCompetitionCardViewModel Competition { get; set; } = new();
    public List<ProfessionalStandingGroupViewModel> Standings { get; set; } = new();
    public List<ProfessionalMatchViewModel> UpcomingMatches { get; set; } = new();
    public bool LoadFailed { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class ProfessionalStandingGroupViewModel
{
    public string Name { get; set; } = "";
    public List<ProfessionalStandingEntryViewModel> Entries { get; set; } = new();
}

public sealed class ProfessionalStandingEntryViewModel
{
    public int Position { get; set; }
    public string TeamExternalId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string? TeamLogo { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int GoalDifference { get; set; }
    public int Points { get; set; }
}

public sealed class ProfessionalMatchViewModel
{
    public string ExternalId { get; set; } = "";
    public DateTimeOffset Date { get; set; }
    public string Status { get; set; } = "";
    public string StatusDetail { get; set; } = "";
    public string HomeTeamName { get; set; } = "";
    public string? HomeTeamLogo { get; set; }
    public string AwayTeamName { get; set; } = "";
    public string? AwayTeamLogo { get; set; }
    public string? Venue { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}
