using System;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public class CreateLeagueResponse
    {
        public Guid Id { get; }
        public string Slug { get; }

        public CreateLeagueResponse(Guid id, string slug)
        {
            Id = id;
            Slug = slug;
        }
    }
}
