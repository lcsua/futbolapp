using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class FieldBlackout : Entity
    {
        public Guid FieldId { get; private set; }
        public virtual Field Field { get; private set; }

        public DateOnly Date { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public string Reason { get; private set; }

        protected FieldBlackout() { }

        public FieldBlackout(Field field, DateOnly date, TimeOnly startTime, TimeOnly endTime, string reason = null)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            FieldId = field.Id;
            Date = date;
            StartTime = startTime;
            EndTime = endTime;
            Reason = reason ?? string.Empty;
        }
    }
}
