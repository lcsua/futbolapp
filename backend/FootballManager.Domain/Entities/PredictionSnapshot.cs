using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Probability snapshot for one team after a round. No foreign keys: the ids can
/// come from another database when predictions are computed from a remote league.
/// </summary>
public class PredictionSnapshot : Entity
{
    public string LeagueSlug { get; private set; } = string.Empty;
    public string SeasonSlug { get; private set; } = string.Empty;
    public string DivisionSlug { get; private set; } = string.Empty;
    public Guid TeamId { get; private set; }
    public int CutoffRound { get; private set; }
    public bool Reconstructed { get; private set; }
    public double TitleProbability { get; private set; }
    public double PodiumProbability { get; private set; }
    public double ExpectedPosition { get; private set; }
    public double ExpectedPoints { get; private set; }
    public string ModelVersion { get; private set; } = string.Empty;
    public DateTime CalculatedAt { get; private set; }

    public static PredictionSnapshot Create(
        string leagueSlug,
        string seasonSlug,
        string divisionSlug,
        Guid teamId,
        int cutoffRound,
        bool reconstructed,
        double titleProbability,
        double podiumProbability,
        double expectedPosition,
        double expectedPoints,
        string modelVersion,
        DateTime calculatedAt) =>
        new()
        {
            LeagueSlug = leagueSlug,
            SeasonSlug = seasonSlug,
            DivisionSlug = divisionSlug,
            TeamId = teamId,
            CutoffRound = cutoffRound,
            Reconstructed = reconstructed,
            TitleProbability = titleProbability,
            PodiumProbability = podiumProbability,
            ExpectedPosition = expectedPosition,
            ExpectedPoints = expectedPoints,
            ModelVersion = modelVersion,
            CalculatedAt = calculatedAt
        };

    public void Refresh(
        double titleProbability,
        double podiumProbability,
        double expectedPosition,
        double expectedPoints,
        DateTime calculatedAt)
    {
        TitleProbability = titleProbability;
        PodiumProbability = podiumProbability;
        ExpectedPosition = expectedPosition;
        ExpectedPoints = expectedPoints;
        CalculatedAt = calculatedAt;
        UpdateTimestamp();
    }
}
