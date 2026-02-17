using System;

namespace FootballManager.Application.UseCases.Leagues.CreateDivision
{
    public class CreateDivisionResponse
    {
        public Guid Id { get; }

        public CreateDivisionResponse(Guid id)
        {
            Id = id;
        }
    }
}
