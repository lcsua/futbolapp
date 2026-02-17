using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateTeam
{
    public class UpdateTeamRequest
    {
        public Guid LeagueId { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? LogoUrl { get; set; }
        public string? Email { get; set; }
        public int? FoundedYear { get; set; }
        public string? DelegateName { get; set; }
        public string? DelegateContact { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
