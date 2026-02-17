using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.SaveFieldAvailabilities
{
    public class FieldAvailabilitySlot
    {
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SaveFieldAvailabilitiesRequest
    {
        public Guid LeagueId { get; set; }
        public Guid FieldId { get; set; }
        public Guid UserId { get; set; }
        public List<FieldAvailabilitySlot> Slots { get; set; } = new();
    }
}
