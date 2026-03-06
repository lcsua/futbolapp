using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Matches.GetMatches;

public sealed class GetMatchesResponse
{
    public IReadOnlyList<MatchRoundGroupDto> Rounds { get; }

    public GetMatchesResponse(IReadOnlyList<MatchRoundGroupDto> rounds)
    {
        Rounds = rounds ?? new List<MatchRoundGroupDto>();
    }
}

public sealed class MatchRoundGroupDto
{
    public int RoundNumber { get; }
    public string DivisionName { get; }
    public IReadOnlyList<MatchListItemDto> Matches { get; }

    public MatchRoundGroupDto(int roundNumber, string divisionName, IReadOnlyList<MatchListItemDto> matches)
    {
        RoundNumber = roundNumber;
        DivisionName = divisionName;
        Matches = matches ?? new List<MatchListItemDto>();
    }
}

public sealed class MatchListItemDto
{
    public Guid Id { get; }
    public Guid DivisionSeasonId { get; }
    public string DivisionName { get; }
    public int RoundNumber { get; }
    public string HomeTeamName { get; }
    public Guid HomeTeamId { get; }
    public string AwayTeamName { get; }
    public Guid AwayTeamId { get; }
    public int? HomeScore { get; }
    public int? AwayScore { get; }
    public string Status { get; }
    public string KickoffTime { get; }
    public string MatchDate { get; }
    public string FieldName { get; }
    public string HomeTeamLogoUrl { get; }
    public string AwayTeamLogoUrl { get; }

    public MatchListItemDto(Guid id, Guid divisionSeasonId, string divisionName, int roundNumber,
        string homeTeamName, Guid homeTeamId, string awayTeamName, Guid awayTeamId,
        int? homeScore, int? awayScore, string status, string kickoffTime, string matchDate, string fieldName,
        string homeTeamLogoUrl = null, string awayTeamLogoUrl = null)
    {
        Id = id;
        DivisionSeasonId = divisionSeasonId;
        DivisionName = divisionName;
        RoundNumber = roundNumber;
        HomeTeamName = homeTeamName;
        HomeTeamId = homeTeamId;
        AwayTeamName = awayTeamName;
        AwayTeamId = awayTeamId;
        HomeScore = homeScore;
        AwayScore = awayScore;
        Status = status;
        KickoffTime = kickoffTime;
        MatchDate = matchDate;
        FieldName = fieldName;
        HomeTeamLogoUrl = homeTeamLogoUrl;
        AwayTeamLogoUrl = awayTeamLogoUrl;
    }
}
