using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateClub
{
    public class UpdateClubRequest
    {
        public Guid LeagueId { get; set; }
        public Guid ClubId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
    }
}
