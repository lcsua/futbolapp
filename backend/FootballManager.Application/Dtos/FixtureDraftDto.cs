namespace FootballManager.Application.Dtos;

public sealed record FixtureDraftDto(
    IReadOnlyList<FixtureDraftRoundDto> Rounds
);

public sealed record FixtureDraftRoundDto(
    int RoundNumber,
    DateOnly MatchDate,
    IReadOnlyList<FixtureDraftMatchDto> Matches
);

public sealed record FixtureDraftMatchDto(
    Guid DivisionSeasonId,
    string DivisionName,
    Guid HomeTeamDivisionSeasonId,
    string HomeTeamName,
    Guid AwayTeamDivisionSeasonId,
    string AwayTeamName,
    Guid FieldId,
    string FieldName,
    DateOnly Date,
    TimeOnly KickoffTime
);
