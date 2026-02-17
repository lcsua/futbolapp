using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class FieldAvailability : Entity
    {
        public Guid FieldId { get; private set; }
        public virtual Field Field { get; private set; }

        public int DayOfWeek { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public TimeOnly EndTime { get; private set; }
        public bool IsActive { get; private set; }

        protected FieldAvailability() { }

        public FieldAvailability(Field field, int dayOfWeek, TimeOnly startTime, TimeOnly endTime)
        {
            Field = field ?? throw new ArgumentNullException(nameof(field));
            FieldId = field.Id;
            if (dayOfWeek < 0 || dayOfWeek > 6)
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week must be 0-6 (Sunday-Saturday).");
            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
            IsActive = true;
        }

        public void SetTimes(TimeOnly startTime, TimeOnly endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
            UpdateTimestamp();
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            UpdateTimestamp();
        }
    }
}
