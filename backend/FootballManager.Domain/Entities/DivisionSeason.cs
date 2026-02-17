using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class DivisionSeason : Entity
    {
        public Guid SeasonId { get; private set; }
        public virtual Season Season { get; private set; }

        public Guid DivisionId { get; private set; }
        public virtual Division Division { get; private set; }

        private readonly List<TeamDivisionSeason> _teamAssignments = new();
        public virtual IReadOnlyCollection<TeamDivisionSeason> TeamAssignments => _teamAssignments.AsReadOnly();

        private readonly List<Fixture> _fixtures = new();
        public virtual IReadOnlyCollection<Fixture> Fixtures => _fixtures.AsReadOnly();

        protected DivisionSeason() { }

        public DivisionSeason(Season season, Division division)
        {
            Season = season ?? throw new ArgumentNullException(nameof(season));
            SeasonId = season.Id;
            Division = division ?? throw new ArgumentNullException(nameof(division));
            DivisionId = division.Id;
        }
    }
}
