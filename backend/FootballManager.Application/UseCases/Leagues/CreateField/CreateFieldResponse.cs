using System;

namespace FootballManager.Application.UseCases.Leagues.CreateField
{
    public class CreateFieldResponse
    {
        public Guid Id { get; }

        public CreateFieldResponse(Guid id)
        {
            Id = id;
        }
    }
}
