using System;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public class CreateLeagueResponse
    {
        public Guid Id { get; }

        public CreateLeagueResponse(Guid id)
        {
            Id = id;
        }
    }
}
