using System;

namespace FootballManager.Application.UseCases.Leagues.CreateSeason
{
    public class CreateSeasonRequest
    {
        public Guid LeagueId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public CreateSeasonRequest(Guid leagueId, Guid userId, string name, DateOnly startDate, DateOnly? endDate = null)
        {
            LeagueId = leagueId;
            UserId = userId;
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
        }

        public CreateSeasonRequest() { }
    }
}
