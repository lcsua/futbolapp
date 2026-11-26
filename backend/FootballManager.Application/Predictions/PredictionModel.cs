namespace FootballManager.Application.Predictions;

/// <summary>
/// Poisson model used to score the remaining matches, then fed to the official
/// standings projection. No home advantage: many amateur leagues share grounds.
///
/// Formulas (version <see cref="Version"/>):
///
/// League mean goals per team appearance:
///   μ = totalGoals / appearances, or <see cref="GoalsPrior"/> when nobody has played.
///
/// Each team appearance is ordered by round. The latest has weight 1 and the one
/// k matches older has weight <see cref="RecencyDecay"/>^k.
///
/// Two rates are blended before shrinkage:
///   recent  = weighted goals in the last <see cref="RecentWindow"/> matches
///   season  = weighted goals over every match
///   blend   = <see cref="RecentBlend"/> * recent + <see cref="SeasonBlend"/> * season
///
/// A second pass divides goals by the opponent's pass-1 rate, so scoring against a
/// strong defense counts more than scoring against a weak one. Unknown opponents
/// use μ. Rates are then shrunk toward μ with <see cref="PriorMatches"/> of prior
/// weight, which keeps a team with one blowout close to the division:
///   shrunk = (n * blend + PriorMatches * μ) / (n + PriorMatches)
///
/// Strengths are shrunk / μ, clamped to [<see cref="MinStrength"/>, <see cref="MaxStrength"/>].
/// Expected goals, with home advantage fixed at <see cref="HomeAdvantage"/> (1 = none):
///   λ_local  = clamp(μ * attack_local  * defense_visita * HomeAdvantage)
///   λ_visita = clamp(μ * attack_visita * defense_local)
///
/// The scoreline is an independent Poisson pair, truncated at <see cref="MaxGoals"/>
/// and renormalized. Monte Carlo draws <see cref="Simulations"/> scorelines with a
/// fixed seed and ranks each sample with the official tie-break
/// (points, goal difference, goals for).
///
/// Predictions are withheld until the division has <see cref="MinCompletedMatches"/>
/// completed matches. Championship progress is not treated as a validated accuracy.
/// Playoffs, promotion and relegation are not invented: champion means first place,
/// and those extra probabilities stay unpublished until the rules exist.
/// A future per-round history can store <see cref="PredictionHistoryPoint"/>; nothing
/// is persisted today.
/// </summary>
public static class PredictionModel
{
    public const string Version = "poisson-mc-1";
    public const int Simulations = 10_000;
    public const int PriorMatches = 6;
    public const double RecencyDecay = 0.82;
    public const int RecentWindow = 5;
    public const double RecentBlend = 0.60;
    public const double SeasonBlend = 0.40;
    public const double GoalsPrior = 1.35;
    public const double HomeAdvantage = 1.0;
    public const int MaxGoals = 10;
    public const int MinCompletedMatches = 4;
    public const double MinStrength = 0.35;
    public const double MaxStrength = 2.80;
    public const double MinLambda = 0.20;
    public const double MaxLambda = 4.50;
    public const int Seed = 20261126;

    public static PredictionOptions Default { get; } = new();
}

public sealed record PredictionOptions
{
    public int Simulations { get; init; } = PredictionModel.Simulations;
    public int PriorMatches { get; init; } = PredictionModel.PriorMatches;
    public double RecencyDecay { get; init; } = PredictionModel.RecencyDecay;
    public int RecentWindow { get; init; } = PredictionModel.RecentWindow;
    public double RecentBlend { get; init; } = PredictionModel.RecentBlend;
    public double GoalsPrior { get; init; } = PredictionModel.GoalsPrior;
    public double HomeAdvantage { get; init; } = PredictionModel.HomeAdvantage;
    public int MaxGoals { get; init; } = PredictionModel.MaxGoals;
    public int MinCompletedMatches { get; init; } = PredictionModel.MinCompletedMatches;
    public int Seed { get; init; } = PredictionModel.Seed;
}

/// <summary>Reserved shape for a future per-round probability history. Not stored.</summary>
public sealed record PredictionHistoryPoint(int Round, DateTime GeneratedAtUtc, string ModelVersion, double PlayedRatio);
