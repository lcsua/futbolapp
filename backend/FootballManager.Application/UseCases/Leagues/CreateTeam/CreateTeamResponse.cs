using System;

namespace FootballManager.Application.UseCases.Leagues.CreateTeam
{
    public class CreateTeamResponse
    {
        public Guid Id { get; }

        public CreateTeamResponse(Guid id)
        {
            Id = id;
        }
    }
}
