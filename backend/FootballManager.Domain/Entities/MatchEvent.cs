using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class MatchEvent : Entity
    {
        public Guid FixtureId { get; private set; }
        public virtual Fixture Fixture { get; private set; }

        public Guid? PlayerId { get; private set; }
        public virtual Player Player { get; private set; }

        public MatchEventType EventType { get; private set; } // Enum
        public int? Minute { get; private set; }
        public string ExtraInfo { get; private set; }

        protected MatchEvent() { }

        public MatchEvent(Fixture fixture, MatchEventType eventType, int minute, Player player = null)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            FixtureId = fixture.Id;
            EventType = eventType;
            Minute = minute >= 0 ? minute : throw new ArgumentException("Minute cannot be negative.");
            Player = player;
            PlayerId = player?.Id;
        }
    }
}
