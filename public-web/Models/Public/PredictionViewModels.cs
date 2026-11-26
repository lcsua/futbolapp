namespace PublicWeb.Models.Public;

public class PredictionsPageViewModel
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public string? Warning { get; set; }
    public List<PredictionDivisionViewModel> Divisions { get; set; } = new();
}

public class PredictionDivisionViewModel
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public bool Supported { get; set; }
    public string? Limitation { get; set; }
    public bool Published { get; set; }
    public bool Finished { get; set; }
    public string? UnpublishedReason { get; set; }
    public string Maturity { get; set; } = string.Empty;
    public string MaturityNote { get; set; } = string.Empty;
    public int CompletedMatches { get; set; }
    public int PendingMatches { get; set; }
    public int PendingRounds { get; set; }
    public double PlayedRatio { get; set; }
    public int Simulations { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public bool ChampionMeansFirstPlace { get; set; }
    public string ChampionNote { get; set; } = string.Empty;
    public bool PromotionSupported { get; set; }
    public string PromotionNote { get; set; } = string.Empty;
    public int? NextRoundNumber { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public List<PredictionTeamViewModel> Teams { get; set; } = new();
    public List<PredictionMatchViewModel> NextRound { get; set; } = new();

    public int PlayedPercent => (int)Math.Round(Math.Clamp(PlayedRatio, 0, 1) * 100, MidpointRounding.AwayFromZero);
}

public class PredictionTeamViewModel
{
    public TeamViewModel Team { get; set; } = new();
    public int OfficialPosition { get; set; }
    public int CurrentPoints { get; set; }
    public int Played { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public double ExpectedPoints { get; set; }
    public double ExpectedPosition { get; set; }
    public int ProjectedRank { get; set; }
    public int PlacesDelta { get; set; }
    public double TitleProbability { get; set; }
    public string TitleLabel { get; set; } = string.Empty;
    public string TitleKind { get; set; } = string.Empty;
    public bool MathematicallyCrowned { get; set; }
    public bool MathematicallyEliminated { get; set; }
    public string Form { get; set; } = string.Empty;
    public List<double> PositionProbability { get; set; } = new();
}

public class PredictionMatchViewModel
{
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public DateTime? Kickoff { get; set; }
    public TeamViewModel Home { get; set; } = new();
    public TeamViewModel Away { get; set; } = new();
    public double HomeWin { get; set; }
    public double Draw { get; set; }
    public double AwayWin { get; set; }
    public string HomeWinLabel { get; set; } = string.Empty;
    public string DrawLabel { get; set; } = string.Empty;
    public string AwayWinLabel { get; set; } = string.Empty;
    public int LikelyHomeGoals { get; set; }
    public int LikelyAwayGoals { get; set; }
    public string HomeForm { get; set; } = string.Empty;
    public string AwayForm { get; set; } = string.Empty;
}
