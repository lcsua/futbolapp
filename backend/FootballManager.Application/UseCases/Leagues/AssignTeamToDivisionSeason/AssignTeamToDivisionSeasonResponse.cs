using System;

namespace FootballManager.Application.UseCases.Leagues.AssignTeamToDivisionSeason
{
    public class AssignTeamToDivisionSeasonResponse
    {
        public Guid TeamDivisionSeasonId { get; }

        public AssignTeamToDivisionSeasonResponse(Guid teamDivisionSeasonId)
        {
            TeamDivisionSeasonId = teamDivisionSeasonId;
        }
    }
}
