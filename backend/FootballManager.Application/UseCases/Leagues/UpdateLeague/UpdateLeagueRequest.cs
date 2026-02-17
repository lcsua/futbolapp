using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateLeague
{
    public class UpdateLeagueRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
    }
}
