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
        /// <summary>Show on public-web. Defaults to false so new seasons stay draft until published.</summary>
        public bool IsPublic { get; set; }

        public CreateSeasonRequest(Guid leagueId, Guid userId, string name, DateOnly startDate, DateOnly? endDate = null, bool isPublic = false)
        {
            LeagueId = leagueId;
            UserId = userId;
            Name = name;
            StartDate = startDate;
            EndDate = endDate;
            IsPublic = isPublic;
        }

        public CreateSeasonRequest() { }
    }
}
