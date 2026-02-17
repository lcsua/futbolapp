using System;

namespace FootballManager.Application.UseCases.Leagues.AssignDivisionToSeason
{
    public class AssignDivisionToSeasonResponse
    {
        public Guid DivisionSeasonId { get; }

        public AssignDivisionToSeasonResponse(Guid divisionSeasonId)
        {
            DivisionSeasonId = divisionSeasonId;
        }
    }
}
