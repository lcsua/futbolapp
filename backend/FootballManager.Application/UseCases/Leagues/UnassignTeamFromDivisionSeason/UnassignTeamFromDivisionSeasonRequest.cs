using System;

namespace FootballManager.Application.UseCases.Leagues.UnassignTeamFromDivisionSeason
{
    public class UnassignTeamFromDivisionSeasonRequest
    {
        public Guid LeagueId { get; set; }
        public Guid SeasonId { get; set; }
        public Guid DivisionId { get; set; }
        public Guid TeamId { get; set; }
        public Guid UserId { get; set; }
    }
}
