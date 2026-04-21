using System;

namespace FootballManager.Application.UseCases.Leagues.CreateClub
{
    public class CreateClubRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}
