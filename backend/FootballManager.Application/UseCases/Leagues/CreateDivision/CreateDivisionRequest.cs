using System;

namespace FootballManager.Application.UseCases.Leagues.CreateDivision
{
    public class CreateDivisionRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
