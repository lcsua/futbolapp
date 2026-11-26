namespace FootballManager.Application.Predictions;

public sealed record PredictionSide(Guid TeamId, string Name, bool Counts);

public sealed record PredictionFixture(
    Guid Id,
    int Round,
    DateTime? Kickoff,
    PredictionSide Home,
    PredictionSide Away,
    int? HomeGoals,
    int? AwayGoals,
    bool Completed);

public sealed record PredictionBaseline(
    Guid TeamId,
    string Name,
    int Position,
    int Played,
    int GoalsFor,
    int GoalsAgainst,
    int Points);

public sealed record PredictionDivision(
    Guid DivisionId,
    string DivisionName,
    IReadOnlyList<PredictionBaseline> Baseline,
    IReadOnlyList<PredictionFixture> Fixtures);
