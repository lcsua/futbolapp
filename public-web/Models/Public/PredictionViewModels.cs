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
    public PredictionRaceViewModel? Race { get; set; }
    public List<PredictionPathViewModel> Paths { get; set; } = new();
    public List<PredictionWhatIfViewModel> WhatIfs { get; set; } = new();
    public PredictionJointWhatIfViewModel? JointWhatIfs { get; set; }
    public PredictionHistoryViewModel? History { get; set; }
    public List<PredictionDecisiveMatchViewModel> DecisiveMatches { get; set; } = new();

    public int PlayedPercent => (int)Math.Round(Math.Clamp(PlayedRatio, 0, 1) * 100, MidpointRounding.AwayFromZero);
}

public class PredictionDecisiveMatchViewModel
{
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public TeamViewModel Home { get; set; } = new();
    public TeamViewModel Away { get; set; } = new();
    public string ImpactLevel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double MaxTitleSwing { get; set; }
    public double HomeSwing { get; set; }
    public double AwaySwing { get; set; }
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
    public double PodiumProbability { get; set; }
    public int PointsLow { get; set; }
    public int PointsHigh { get; set; }
    public int PositionLow { get; set; }
    public int PositionHigh { get; set; }
    public double? TitleChange { get; set; }
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
    public string FavoriteLabel { get; set; } = string.Empty;
    public string ToneLabel { get; set; } = string.Empty;
}

public class PredictionRaceViewModel
{
    public string Kind { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int PointGap { get; set; }
    public int MatchesLeft { get; set; }
    public int PointsInPlay { get; set; }
    public List<Guid> CandidateIds { get; set; } = new();
}

public class PredictionPathViewModel
{
    public Guid TeamId { get; set; }
    public string Stance { get; set; } = string.Empty;
    public string StanceLabel { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int CurrentPoints { get; set; }
    public int MaxPoints { get; set; }
    public int RivalMax { get; set; }
    public int? PointsToClinch { get; set; }
    public string PointsNote { get; set; } = string.Empty;
    public int BestPosition { get; set; }
    public int WorstPosition { get; set; }
    public string PositionNote { get; set; } = string.Empty;
    public bool CanClinchNextRound { get; set; }
    public string ClinchNote { get; set; } = string.Empty;
    public List<PredictionCampaignViewModel> Campaigns { get; set; } = new();
}

public class PredictionCampaignViewModel
{
    public string Marks { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool SecuresTitle { get; set; }
}

public class PredictionWhatIfViewModel
{
    public Guid TeamId { get; set; }
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public double WinTitle { get; set; }
    public double DrawTitle { get; set; }
    public double LossTitle { get; set; }
    public bool WinReliable { get; set; }
    public bool DrawReliable { get; set; }
    public bool LossReliable { get; set; }
    public List<PredictionWhatIfRivalViewModel> Rivals { get; set; } = new();
}

public class PredictionWhatIfRivalViewModel
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double IfWin { get; set; }
    public double IfDraw { get; set; }
    public double IfLoss { get; set; }
}

public class PredictionJointWhatIfViewModel
{
    public Guid DefaultTeamAId { get; set; }
    public Guid DefaultTeamBId { get; set; }
    public List<PredictionJointWhatIfTeamViewModel> Options { get; set; } = new();
    public List<PredictionJointWhatIfPairViewModel> Pairs { get; set; } = new();
}

public class PredictionJointWhatIfTeamViewModel
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public bool IsHome { get; set; }
    public int CurrentPoints { get; set; }
    public double TitleProbability { get; set; }
}

public class PredictionJointWhatIfPairViewModel
{
    public Guid TeamAId { get; set; }
    public Guid TeamBId { get; set; }
    public bool HeadToHead { get; set; }
    public List<PredictionJointWhatIfCellViewModel> Cells { get; set; } = new();
}

public class PredictionJointWhatIfCellViewModel
{
    public int OutcomeA { get; set; }
    public int OutcomeB { get; set; }
    public double TitleA { get; set; }
    public double TitleB { get; set; }
    public bool Reliable { get; set; }
    public int Samples { get; set; }
    public List<PredictionJointWhatIfOtherViewModel> Others { get; set; } = new();
}

public class PredictionJointWhatIfOtherViewModel
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double TitleProbability { get; set; }
}

public class PredictionHistoryViewModel
{
    public bool Available { get; set; }
    public bool Reconstructed { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<PredictionHistoryRoundViewModel> Rounds { get; set; } = new();
}

public class PredictionHistoryRoundViewModel
{
    public int Round { get; set; }
    public bool Reconstructed { get; set; }
    public List<PredictionHistoryTeamViewModel> Teams { get; set; } = new();
}

public class PredictionHistoryTeamViewModel
{
    public Guid TeamId { get; set; }
    public double TitleProbability { get; set; }
    public double PodiumProbability { get; set; }
    public double ExpectedPosition { get; set; }
    public double ExpectedPoints { get; set; }
}
