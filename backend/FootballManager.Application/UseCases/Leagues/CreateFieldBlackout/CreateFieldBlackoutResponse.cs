using System;

namespace FootballManager.Application.UseCases.Leagues.CreateFieldBlackout
{
    public class CreateFieldBlackoutResponse
    {
        public Guid Id { get; }

        public CreateFieldBlackoutResponse(Guid id)
        {
            Id = id;
        }
    }
}
