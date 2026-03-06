using System;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class MatchIncident : Entity
    {
        public Guid FixtureId { get; private set; }
        public virtual Fixture Fixture { get; private set; }

        public int Minute { get; private set; }
        public Guid? TeamId { get; private set; }
        public virtual Team Team { get; private set; }
        public string PlayerName { get; private set; }
        public MatchIncidentType IncidentType { get; private set; }
        public string Notes { get; private set; }

        protected MatchIncident() { }

        public MatchIncident(Fixture fixture, int minute, Guid? teamId, string playerName, MatchIncidentType incidentType, string notes = null)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            FixtureId = fixture.Id;
            if (minute < 0)
                throw new ArgumentException("Minute must be >= 0.", nameof(minute));
            Minute = minute;
            TeamId = teamId;
            PlayerName = playerName ?? string.Empty;
            IncidentType = incidentType;
            Notes = notes ?? string.Empty;
        }
    }
}
