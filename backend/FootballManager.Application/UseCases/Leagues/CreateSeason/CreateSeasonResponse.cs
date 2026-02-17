using System;

namespace FootballManager.Application.UseCases.Leagues.CreateSeason
{
    public class CreateSeasonResponse
    {
        public Guid Id { get; }

        public CreateSeasonResponse(Guid id)
        {
            Id = id;
        }
    }
}
