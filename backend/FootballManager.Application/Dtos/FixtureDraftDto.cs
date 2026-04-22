namespace FootballManager.Application.Dtos;

public sealed record FixtureDraftDto(
    IReadOnlyList<FixtureDraftRoundDto> Rounds
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
    TimeOnly? KickoffTime
);

public sealed record FixtureDraftByeDto(
    Guid DivisionSeasonId,
    string DivisionName,
    Guid TeamDivisionSeasonId,
    string TeamName
);
