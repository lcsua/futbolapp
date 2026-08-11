using System;

namespace FootballManager.Application.UseCases.Leagues.SeedLeagueDocumentDefaults
{
    public class SeedLeagueDocumentDefaultsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid? UserId { get; set; }
        public bool RequireMembership { get; set; } = true;
    }
}
