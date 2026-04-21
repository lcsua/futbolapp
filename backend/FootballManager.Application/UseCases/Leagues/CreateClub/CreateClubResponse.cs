using System;

namespace FootballManager.Application.UseCases.Leagues.CreateClub
{
    public class CreateClubResponse
    {
        public Guid Id { get; }

        public CreateClubResponse(Guid id)
        {
            Id = id;
        }
    }
}
