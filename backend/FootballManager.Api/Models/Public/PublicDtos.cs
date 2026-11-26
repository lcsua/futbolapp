using System;
using System.Collections.Generic;

namespace FootballManager.Api.Models.Public;

public class PublicAdvertisementDto
{
    public Guid Id { get; set; }
    public string AdvertiserName { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty;
    public string? DesktopImageUrl { get; set; }
    public string? MobileImageUrl { get; set; }
    public string? TargetUrl { get; set; }
}

public class LeaguePublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? FontKey { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? TeamHeroImageUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? InstagramUrl { get; set; }
    public string? YouTubeUrl { get; set; }
    public string? TikTokUrl { get; set; }
}

public class TeamPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? LogoUrl { get; set; }
    /// <summary>Compact WebP thumb for lists; null when unavailable.</summary>
    public string? LogoThumbUrl { get; set; }
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
    public string? LeagueSlug { get; set; }
    public string? SeasonSlug { get; set; }
    public string? FieldName { get; set; }
    public int RoundNumber { get; set; }
    public string? DivisionName { get; set; }
    public List<MatchIncidentPublicDto> Incidents { get; set; } = new();
}

public class MatchIncidentPublicDto
{
    public int? Minute { get; set; }
    public Guid? TeamId { get; set; }
    public string TeamName { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class SeasonPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateOnly? EndDate { get; set; }
    public bool IsActive { get; set; }
    public List<DivisionPublicDto> Divisions { get; set; } = new();
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
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public string? DivisionName { get; set; }
}

public class TeamScorerPublicDto
{
    public Guid? PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public int Goals { get; set; }
    public string? PhotoUrl { get; set; }
}

public class TeamSummaryPublicDto
{
    public TeamPublicDto Team { get; set; } = new();
    public LeaguePublicDto? League { get; set; }
    public SeasonPublicDto? Season { get; set; }
    public List<SeasonPublicDto> ActiveSeasons { get; set; } = new();
    public List<MatchPublicDto> NextMatches { get; set; } = new();
    public List<MatchPublicDto> LastResults { get; set; } = new();
    public List<TeamScorerPublicDto> Scorers { get; set; } = new();
    public StandingSummaryDto? Standing { get; set; }
    public int PageSize { get; set; } = 5;
    public int NextMatchesPage { get; set; } = 1;
    public int NextMatchesTotal { get; set; }
    public int LastResultsPage { get; set; } = 1;
    public int LastResultsTotal { get; set; }
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
    public int? DefaultRound { get; set; }
    public List<T> Data { get; set; } = new();
}

public class SeasonGroupedDto<T>
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public List<DivisionGroupDto<T>> Divisions { get; set; } = new();
}

public class MatchdayGroupDto
{
    public int Round { get; set; }
    public List<MatchPublicDto> Matches { get; set; } = new();
}

public class LeagueDocumentsPublicDto
{
    public List<LeagueDocumentCategoryPublicDto> Categories { get; set; } = new();
}

public class LeagueDocumentCategoryPublicDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool RequiresDocumentDate { get; set; }
    public int SortOrder { get; set; }
    public List<LeagueDocumentPublicDto> Documents { get; set; } = new();
}

public class SimulatorPublicDto
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public bool Supported { get; set; }
    public string? Limitation { get; set; }
    public string? Warning { get; set; }
    public SimulatorRulesPublicDto Rules { get; set; } = new();
    public List<SimulatorDivisionPublicDto> Divisions { get; set; } = new();
}

public class SimulatorRulesPublicDto
{
    public int WinPoints { get; set; } = 3;
    public int DrawPoints { get; set; } = 1;
    public int LossPoints { get; set; }
    public List<string> TieBreakers { get; set; } = new();
    public List<SimulatorAdjustmentPublicDto> Adjustments { get; set; } = new();
}

public class SimulatorAdjustmentPublicDto
{
    public Guid TeamId { get; set; }
    public int Points { get; set; }
}

public class SimulatorDivisionPublicDto
{
    public string DivisionName { get; set; } = string.Empty;
    public string DivisionSlug { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public List<SimulatorOfficialRowPublicDto> Official { get; set; } = new();
    public List<SimulatorRoundPublicDto> Rounds { get; set; } = new();
}

public class SimulatorOfficialRowPublicDto
{
    public int Position { get; set; }
    public TeamPublicDto Team { get; set; } = new();
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Points { get; set; }
}

public class SimulatorRoundPublicDto
{
    public int Round { get; set; }
    public List<SimulatorMatchPublicDto> Matches { get; set; } = new();
    public List<SimulatorByePublicDto> Byes { get; set; } = new();
}

public class SimulatorMatchPublicDto
{
    public Guid Id { get; set; }
    public int Round { get; set; }
    public DateTime? Kickoff { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public TeamPublicDto Home { get; set; } = new();
    public TeamPublicDto Away { get; set; } = new();
    public bool HomeCounts { get; set; }
    public bool AwayCounts { get; set; }
}

public class SimulatorByePublicDto
{
    public TeamPublicDto Team { get; set; } = new();
}

public class PredictionsPublicDto
{
    public string SeasonName { get; set; } = string.Empty;
    public string SeasonSlug { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public string? Warning { get; set; }
    public List<PredictionDivisionPublicDto> Divisions { get; set; } = new();
}

public class PredictionDivisionPublicDto
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
    public List<PredictionTeamPublicDto> Teams { get; set; } = new();
    public List<PredictionMatchPublicDto> NextRound { get; set; } = new();
    public PredictionRacePublicDto? Race { get; set; }
    public List<PredictionPathPublicDto> Paths { get; set; } = new();
    public List<PredictionWhatIfPublicDto> WhatIfs { get; set; } = new();
    public PredictionHistoryPublicDto? History { get; set; }
    public List<PredictionDecisiveMatchPublicDto> DecisiveMatches { get; set; } = new();
}

public class PredictionDecisiveMatchPublicDto
{
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public TeamPublicDto Home { get; set; } = new();
    public TeamPublicDto Away { get; set; } = new();
    public string ImpactLevel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double MaxTitleSwing { get; set; }
    public double HomeSwing { get; set; }
    public double AwaySwing { get; set; }
}

public class PredictionRacePublicDto
{
    public string Kind { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int PointGap { get; set; }
    public int MatchesLeft { get; set; }
    public int PointsInPlay { get; set; }
    public List<Guid> CandidateIds { get; set; } = new();
}

public class PredictionPathPublicDto
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
    public List<PredictionCampaignPublicDto> Campaigns { get; set; } = new();
}

public class PredictionCampaignPublicDto
{
    public string Marks { get; set; } = string.Empty;
    public int Points { get; set; }
    public bool SecuresTitle { get; set; }
}

public class PredictionWhatIfPublicDto
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
    public List<PredictionWhatIfRivalPublicDto> Rivals { get; set; } = new();
}

public class PredictionWhatIfRivalPublicDto
{
    public Guid TeamId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double IfWin { get; set; }
    public double IfDraw { get; set; }
    public double IfLoss { get; set; }
}

public class PredictionHistoryPublicDto
{
    public bool Available { get; set; }
    public bool Reconstructed { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<PredictionHistoryRoundPublicDto> Rounds { get; set; } = new();
}

public class PredictionHistoryRoundPublicDto
{
    public int Round { get; set; }
    public bool Reconstructed { get; set; }
    public List<PredictionHistoryTeamPublicDto> Teams { get; set; } = new();
}

public class PredictionHistoryTeamPublicDto
{
    public Guid TeamId { get; set; }
    public double TitleProbability { get; set; }
    public double PodiumProbability { get; set; }
    public double ExpectedPosition { get; set; }
    public double ExpectedPoints { get; set; }
}

public class PredictionTeamPublicDto
{
    public TeamPublicDto Team { get; set; } = new();
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

public class PredictionMatchPublicDto
{
    public Guid MatchId { get; set; }
    public int Round { get; set; }
    public DateTime? Kickoff { get; set; }
    public TeamPublicDto Home { get; set; } = new();
    public TeamPublicDto Away { get; set; } = new();
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

public class LeagueDocumentPublicDto
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
