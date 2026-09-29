using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.RoundInterzonalMatches;

public sealed record RoundInterzonalTeamDto(Guid TeamDivisionSeasonId, string TeamName, bool IsBusy);

public sealed record RoundInterzonalZoneDto(
    Guid DivisionSeasonId,
    Guid DivisionId,
    string DivisionName,
    IReadOnlyList<RoundInterzonalTeamDto> Teams);

public sealed record RoundInterzonalPairDto(
    Guid? FixtureId,
    Guid HomeTeamDivisionSeasonId,
    string HomeTeamName,
    Guid AwayTeamDivisionSeasonId,
    string AwayTeamName,
    bool IsLocked,
    string? FieldName,
    TimeOnly? StartTime);

/// <param name="Zones">Teams flagged busy already play a non-interzonal match that round.</param>
public sealed record RoundInterzonalMatchesDto(
    int RoundNumber,
    DateOnly? RoundDate,
    IReadOnlyList<RoundInterzonalZoneDto> Zones,
    IReadOnlyList<RoundInterzonalPairDto> Pairs,
    IReadOnlyList<string> Warnings);

public sealed record SetRoundInterzonalPairDto(Guid HomeTeamDivisionSeasonId, Guid AwayTeamDivisionSeasonId);
