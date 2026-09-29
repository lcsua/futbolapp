namespace FootballManager.Application.Dtos;

public sealed record FixtureDraftDto(
    IReadOnlyList<FixtureDraftRoundDto> Rounds,
    FixtureReplanDto? Replan = null
);

/// <summary>
/// Present when the draft only re-plans pending matches of some zones from <see cref="FromRound"/> on.
/// Committing it removes <see cref="RemovedFixtureIds"/> and inserts the matches flagged as replanned,
/// leaving played matches and every other division untouched.
/// </summary>
public sealed record FixtureReplanDto(
    int FromRound,
    IReadOnlyList<Guid> DivisionSeasonIds,
    IReadOnlyList<Guid> RemovedFixtureIds,
    IReadOnlyList<string> Warnings
);

public sealed record FixtureDraftRoundDto(
    int RoundNumber,
    DateOnly? MatchDate,
    IReadOnlyList<FixtureDraftMatchDto> Matches,
    IReadOnlyList<FixtureDraftByeDto>? ByeTeams = null
);

public sealed record FixtureDraftMatchDto(
    Guid DivisionSeasonId,
    string DivisionName,
    Guid HomeTeamDivisionSeasonId,
    string HomeTeamName,
    Guid AwayTeamDivisionSeasonId,
    string AwayTeamName,
    Guid? FieldId,
    string? FieldName,
    DateOnly? Date,
    TimeOnly? KickoffTime,
    bool IsInterzonal = false,
    string? AwayDivisionName = null,
    bool IsReplanned = false
);

public sealed record FixtureDraftByeDto(
    Guid DivisionSeasonId,
    string DivisionName,
    Guid TeamDivisionSeasonId,
    string TeamName
);
