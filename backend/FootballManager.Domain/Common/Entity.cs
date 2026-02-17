using System;

namespace FootballManager.Domain.Common
{
    public abstract class Entity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        protected Entity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        protected void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            DeletedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }
    }
}
