namespace PublicWeb.Models.Public;

/// <summary>Finished result used to build form and streaks. Most recent first.</summary>
public sealed class RecentResultInput
{
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
}

public sealed class DivisionStandingInput
{
    public Guid TeamId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Position { get; init; }
    public int Played { get; init; }
    public int Points { get; init; }
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
}

public sealed class MatchSideInput
{
    public Guid TeamId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool HasStanding { get; init; }
    public int Position { get; init; }
    public int Played { get; init; }
    public int Points { get; init; }
    public int Wins { get; init; }
    public int Draws { get; init; }
    public int Losses { get; init; }
    public int GoalsFor { get; init; }
    public int GoalsAgainst { get; init; }
    /// <summary>False when the recent sample could not be loaded. Empty + true means the team has no finished matches.</summary>
    public bool RecentKnown { get; init; }
    public IReadOnlyList<RecentResultInput> Recent { get; init; } = Array.Empty<RecentResultInput>();
}

public sealed class MatchInsightInput
{
    public MatchSideInput Home { get; init; } = new();
    public MatchSideInput Away { get; init; } = new();
    public IReadOnlyList<DivisionStandingInput> Division { get; init; } = Array.Empty<DivisionStandingInput>();
    public string? DivisionName { get; init; }
}

/// <summary>Thresholds for the deterministic insight rules. Defaults are the public V1 cutoffs.</summary>
public sealed class MatchInsightThresholds
{
    public static MatchInsightThresholds Default { get; } = new();

    public int MinConsecutiveWins { get; init; } = 3;
    public int MinUnbeaten { get; init; } = 3;
    public int MinCleanSheets { get; init; } = 3;
    public int MinWinless { get; init; } = 4;
    public int MinLosses { get; init; } = 3;
    public int RecentWindow { get; init; } = 5;
    public int MinRecentSample { get; init; } = 3;
    /// <summary>Share of points taken in the recent window (10 of 15 = 0.66).</summary>
    public double GoodFormRate { get; init; } = 0.66;
    public double PoorFormRate { get; init; } = 0.34;
    public int MinFormContrastGap { get; init; } = 4;
    public int MinPlayedForRates { get; init; } = 5;
    public int MaxPlayedGapForRanks { get; init; } = 2;
    public double MinOffensiveGap { get; init; } = 1.0;
    public double MinOffensiveRatio { get; init; } = 1.5;
    public double NotableGoalsPerGame { get; init; } = 3.0;
    public double HighWinRate { get; init; } = 0.70;
    public int TopClashPositions { get; init; } = 5;
    public int ClosePointsGap { get; init; } = 3;
    public int MaxInsights { get; init; } = 4;
}

public sealed class MatchInsight
{
    public string Type { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    /// <summary>flame, shield, swords, trend, trophy, scale, contrast.</summary>
    public string Icon { get; init; } = "trend";
    public Guid? TeamId { get; init; }
}

public sealed class TeamArrivalDisplay
{
    public bool HasStanding { get; init; }
    public int Position { get; init; }
    public int Points { get; init; }
    public IReadOnlyList<char> Form { get; init; } = Array.Empty<char>();
}

public sealed class MatchCardPreview
{
    public TeamArrivalDisplay Home { get; init; } = new();
    public TeamArrivalDisplay Away { get; init; } = new();
    public bool ShowPlace { get; init; }
    public bool ShowForm { get; init; }
}

public sealed class MatchCompareRow
{
    public string Label { get; init; } = string.Empty;
    public int? Home { get; init; }
    public int? Away { get; init; }
    /// <summary>0–1, relative to the larger value of the row.</summary>
    public double HomeShare { get; init; }
    public double AwayShare { get; init; }
}

public sealed class MatchPreviewViewModel
{
    public string HomeName { get; init; } = string.Empty;
    public string AwayName { get; init; } = string.Empty;
    public TeamArrivalDisplay Home { get; init; } = new();
    public TeamArrivalDisplay Away { get; init; } = new();
    public bool ShowPlace { get; init; }
    public bool ShowForm { get; init; }
    public bool HasComparison { get; init; }
    public IReadOnlyList<MatchCompareRow> Rows { get; init; } = Array.Empty<MatchCompareRow>();
    public IReadOnlyList<MatchInsight> Insights { get; init; } = Array.Empty<MatchInsight>();
}

public sealed class FormSlotsViewModel
{
    public IReadOnlyList<char> Letters { get; init; } = Array.Empty<char>();
    public int Slots { get; init; } = 5;
    public bool PadEmpty { get; init; } = true;
    public string? Label { get; init; }
    public string Modifier { get; init; } = string.Empty;
}

public sealed class MatchSideViewModel
{
    public TeamViewModel Team { get; init; } = new();
    public TeamArrivalDisplay? Arrival { get; init; }
    public bool ShowPlace { get; init; }
    public bool ShowForm { get; init; }
    public bool FullPlace { get; init; }
}
