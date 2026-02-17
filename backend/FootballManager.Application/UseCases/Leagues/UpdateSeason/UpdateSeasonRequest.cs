using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateSeason
{
    public class UpdateSeasonRequest
    {
        public Guid LeagueId { get; set; }
        public Guid SeasonId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }
}
