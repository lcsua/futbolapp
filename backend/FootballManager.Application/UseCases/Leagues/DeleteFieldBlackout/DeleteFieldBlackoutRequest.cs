using System;

namespace FootballManager.Application.UseCases.Leagues.DeleteFieldBlackout
{
    public class DeleteFieldBlackoutRequest
    {
        public Guid LeagueId { get; set; }
        public Guid FieldId { get; set; }
        public Guid BlackoutId { get; set; }
        public Guid UserId { get; set; }
    }
}
