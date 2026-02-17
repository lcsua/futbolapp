using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateDivision
{
    public class UpdateDivisionRequest
    {
        public Guid LeagueId { get; set; }
        public Guid DivisionId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
