using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class TeamDivisionSeason : Entity
    {
        public Guid TeamId { get; private set; }
        public virtual Team Team { get; private set; }

        public Guid DivisionSeasonId { get; private set; }
        public virtual DivisionSeason DivisionSeason { get; private set; }

        protected TeamDivisionSeason() { }

        public TeamDivisionSeason(Team team, DivisionSeason divisionSeason)
        {
            Team = team ?? throw new ArgumentNullException(nameof(team));
            TeamId = team.Id;
            DivisionSeason = divisionSeason ?? throw new ArgumentNullException(nameof(divisionSeason));
            DivisionSeasonId = divisionSeason.Id;
        }
    }
}
