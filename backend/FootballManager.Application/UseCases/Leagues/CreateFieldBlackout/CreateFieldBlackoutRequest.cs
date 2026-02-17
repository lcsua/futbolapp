using System;

namespace FootballManager.Application.UseCases.Leagues.CreateFieldBlackout
{
    public class CreateFieldBlackoutRequest
    {
        public Guid LeagueId { get; set; }
        public Guid FieldId { get; set; }
        public Guid UserId { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string? Reason { get; set; }
    }
}
