using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.GetFieldBlackouts
{
    public class FieldBlackoutItem
    {
        public Guid Id { get; }
        public DateOnly Date { get; }
        public TimeOnly StartTime { get; }
        public TimeOnly EndTime { get; }
        public string Reason { get; }

        public FieldBlackoutItem(Guid id, DateOnly date, TimeOnly startTime, TimeOnly endTime, string reason)
        {
            Id = id;
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Reason = reason ?? string.Empty;
        }
    }

    public class GetFieldBlackoutsResponse
    {
        public List<FieldBlackoutItem> Items { get; }

        public GetFieldBlackoutsResponse(List<FieldBlackoutItem> items)
        {
            Items = items ?? new List<FieldBlackoutItem>();
        }
    }
}
