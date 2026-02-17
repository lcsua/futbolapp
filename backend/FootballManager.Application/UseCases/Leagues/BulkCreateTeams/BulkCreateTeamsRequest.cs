using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.BulkCreateTeams
{
    public class BulkCreateTeamsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public List<string> Names { get; set; } = new();
    }
}
