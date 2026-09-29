using FootballManager.Application.Dtos;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Helpers;

public static class FixtureDraftMapper
{
    public static bool IsInterzonal(Fixture f) =>
        f.HomeTeamDivisionSeason != null
        && f.AwayTeamDivisionSeason != null
        && f.HomeTeamDivisionSeason.DivisionSeasonId != f.AwayTeamDivisionSeason.DivisionSeasonId;

    public static FixtureDraftMatchDto ToDraftMatch(Fixture f)
    {
        var interzonal = IsInterzonal(f);
        return new FixtureDraftMatchDto(
            f.DivisionSeasonId,
            f.DivisionSeason.Division.Name,
            f.HomeTeamDivisionSeasonId,
            f.HomeTeamDivisionSeason.Team.CompetitionName,
            f.AwayTeamDivisionSeasonId,
            f.AwayTeamDivisionSeason.Team.CompetitionName,
            f.FieldId,
            f.Field?.Name,
            f.MatchDate,
            f.StartTime,
            IsInterzonal: interzonal,
            AwayDivisionName: interzonal ? f.AwayTeamDivisionSeason.DivisionSeason?.Division?.Name : null);
    }
}
