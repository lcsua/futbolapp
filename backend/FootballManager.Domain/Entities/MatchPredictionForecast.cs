using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Pre-kickoff match forecast. Rows may be refreshed while the match has not started;
/// after kickoff they stay frozen so scoring uses the last valid prediction.
/// </summary>
public class MatchPredictionForecast : Entity
{
    public string LeagueSlug { get; private set; } = string.Empty;
    public string SeasonSlug { get; private set; } = string.Empty;
    public string DivisionSlug { get; private set; } = string.Empty;
    public Guid MatchId { get; private set; }
    public int Round { get; private set; }
    public DateTime? Kickoff { get; private set; }
    public Guid HomeTeamId { get; private set; }
    public Guid AwayTeamId { get; private set; }
    public double HomeWin { get; private set; }
    public double Draw { get; private set; }
    public double AwayWin { get; private set; }
    public int LikelyHomeGoals { get; private set; }
    public int LikelyAwayGoals { get; private set; }
    public double ExpectedHomeGoals { get; private set; }
    public double ExpectedAwayGoals { get; private set; }
    public string ModelVersion { get; private set; } = string.Empty;
    public DateTime CalculatedAt { get; private set; }

    public static MatchPredictionForecast Create(
        string leagueSlug,
        string seasonSlug,
        string divisionSlug,
        Guid matchId,
        int round,
        DateTime? kickoff,
        Guid homeTeamId,
        Guid awayTeamId,
        double homeWin,
        double draw,
        double awayWin,
        int likelyHomeGoals,
        int likelyAwayGoals,
        double expectedHomeGoals,
        double expectedAwayGoals,
        string modelVersion,
        DateTime calculatedAt) =>
        new()
        {
            LeagueSlug = leagueSlug,
            SeasonSlug = seasonSlug,
            DivisionSlug = divisionSlug,
            MatchId = matchId,
            Round = round,
            Kickoff = kickoff,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId,
            HomeWin = homeWin,
            Draw = draw,
            AwayWin = awayWin,
            LikelyHomeGoals = likelyHomeGoals,
            LikelyAwayGoals = likelyAwayGoals,
            ExpectedHomeGoals = expectedHomeGoals,
            ExpectedAwayGoals = expectedAwayGoals,
            ModelVersion = modelVersion,
            CalculatedAt = calculatedAt
        };

    public void RefreshPreKickoff(
        double homeWin,
        double draw,
        double awayWin,
        int likelyHomeGoals,
        int likelyAwayGoals,
        double expectedHomeGoals,
        double expectedAwayGoals,
        DateTime? kickoff,
        DateTime calculatedAt)
    {
        HomeWin = homeWin;
        Draw = draw;
        AwayWin = awayWin;
        LikelyHomeGoals = likelyHomeGoals;
        LikelyAwayGoals = likelyAwayGoals;
        ExpectedHomeGoals = expectedHomeGoals;
        ExpectedAwayGoals = expectedAwayGoals;
        Kickoff = kickoff;
        CalculatedAt = calculatedAt;
    }
}
