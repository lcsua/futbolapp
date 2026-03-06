using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Matches.GetMatchById;

public sealed class GetMatchByIdResponse
{
    public Guid Id { get; }
    public int RoundNumber { get; }
    public string DivisionName { get; }
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
    public IReadOnlyList<MatchIncidentDto> Incidents { get; }

    public GetMatchByIdResponse(Guid id, int roundNumber, string divisionName,
        string homeTeamName, Guid homeTeamId, string awayTeamName, Guid awayTeamId,
        int? homeScore, int? awayScore, string status, string kickoffTime, string matchDate, string fieldName,
        IReadOnlyList<MatchIncidentDto> incidents,
        string homeTeamLogoUrl = null, string awayTeamLogoUrl = null)
    {
        Id = id;
        RoundNumber = roundNumber;
        DivisionName = divisionName;
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
        Incidents = incidents ?? new List<MatchIncidentDto>();
        HomeTeamLogoUrl = homeTeamLogoUrl;
        AwayTeamLogoUrl = awayTeamLogoUrl;
    }
}

public sealed class MatchIncidentDto
{
    public Guid Id { get; }
    public int Minute { get; }
    public Guid? TeamId { get; }
    public string TeamName { get; }
    public string PlayerName { get; }
    public string IncidentType { get; }
    public string Notes { get; }

    public MatchIncidentDto(Guid id, int minute, Guid? teamId, string teamName, string playerName, string incidentType, string notes)
    {
        Id = id;
        Minute = minute;
        TeamId = teamId;
        TeamName = teamName;
        PlayerName = playerName;
        IncidentType = incidentType;
        Notes = notes ?? "";
    }
}
