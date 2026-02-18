using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.GetTeamIdsAssignedToSeason
{
    public class GetTeamIdsAssignedToSeasonResponse
    {
        public List<Guid> TeamIds { get; }

        public GetTeamIdsAssignedToSeasonResponse(List<Guid> teamIds)
        {
            TeamIds = teamIds ?? new List<Guid>();
        }
    }
}
