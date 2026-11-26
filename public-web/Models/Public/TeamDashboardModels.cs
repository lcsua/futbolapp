namespace PublicWeb.Models.Public;

public sealed class TeamDashboard
{
    public TeamNumbersDisplay? Numbers { get; init; }
    public IReadOnlyList<MatchInsight> Insights { get; init; } = Array.Empty<MatchInsight>();
    public TeamEvolutionDisplay? Evolution { get; init; }
    public IReadOnlyList<TeamScorerSpotlight> Scorers { get; init; } = Array.Empty<TeamScorerSpotlight>();
    public bool HasMoreScorers { get; init; }
    public TeamRoadDisplay? Road { get; init; }
}

public sealed class TeamNumbersDisplay
{
    public int Played { get; init; }
    public int Wins { get; init; }
    public int Draws { get; init; }
    public int Losses { get; init; }
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
    public int? WinPercent { get; init; }
    public int? PointsPercent { get; init; }
    public string? GoalsForAverage { get; init; }
    public string? GoalsAgainstAverage { get; init; }
    public string WinsLabel { get; init; } = string.Empty;
    public string DrawsLabel { get; init; } = string.Empty;
    public string LossesLabel { get; init; } = string.Empty;
    public string BalanceLabel { get; init; } = string.Empty;
}

public sealed class TeamEvolutionDisplay
{
    public string Title { get; init; } = string.Empty;
    public int ScaleMax { get; init; }
    public IReadOnlyList<int> Ticks { get; init; } = Array.Empty<int>();
    public IReadOnlyList<TeamEvolutionMatch> Matches { get; init; } = Array.Empty<TeamEvolutionMatch>();
}

public sealed class TeamEvolutionMatch
{
    public string Label { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
    public double ForShare { get; init; }
    public double AgainstShare { get; init; }
}

public sealed class TeamScorerSpotlight
{
    public int Rank { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Goals { get; init; }
    public string GoalsLabel { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public string Initials { get; init; } = string.Empty;
}

public sealed class TeamRoadDisplay
{
    public bool Finished { get; init; }
    public string Headline { get; init; } = string.Empty;
    public string? Detail { get; init; }
    public string? Extra { get; init; }
    public bool HasMoreMatches { get; init; }
    public IReadOnlyList<TeamUpcomingRival> Rivals { get; init; } = Array.Empty<TeamUpcomingRival>();
}

public sealed class TeamUpcomingRival
{
    public TeamViewModel Team { get; init; } = new();
    public MatchViewModel Match { get; init; } = new();
    public string When { get; init; } = string.Empty;
}
