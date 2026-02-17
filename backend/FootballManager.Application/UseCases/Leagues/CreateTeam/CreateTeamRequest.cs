using System;

namespace FootballManager.Application.UseCases.Leagues.CreateTeam
{
    public class CreateTeamRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? Email { get; set; }
    }
}
