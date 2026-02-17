using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.GetFieldAvailabilities
{
    public class FieldAvailabilityItem
    {
        public Guid Id { get; }
        public int DayOfWeek { get; }
        public TimeOnly StartTime { get; }
        public TimeOnly EndTime { get; }
        public bool IsActive { get; }

        public FieldAvailabilityItem(Guid id, int dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isActive)
        {
            Id = id;
            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
            IsActive = isActive;
        }
    }

    public class GetFieldAvailabilitiesResponse
    {
        public List<FieldAvailabilityItem> Items { get; }

        public GetFieldAvailabilitiesResponse(List<FieldAvailabilityItem> items)
        {
            Items = items ?? new List<FieldAvailabilityItem>();
        }
    }
}
