using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.BulkCreateTeams
{
    public class BulkCreateTeamsResponse
    {
        public List<Guid> CreatedIds { get; }

        public BulkCreateTeamsResponse(List<Guid> createdIds)
        {
            CreatedIds = createdIds ?? new List<Guid>();
        }
    }
}
