using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Result : Entity
    {
        public Guid FixtureId { get; private set; }
        public virtual Fixture Fixture { get; private set; }

        public int HomeTeamGoals { get; private set; }
        public int AwayTeamGoals { get; private set; }
        public string Notes { get; private set; }

        protected Result() { }

        public Result(Fixture fixture, int homeGoals, int awayGoals)
        {
            Fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            FixtureId = fixture.Id;
            HomeTeamGoals = homeGoals;
            AwayTeamGoals = awayGoals;
        }

        public void UpdateScore(int homeGoals, int awayGoals)
        {
            HomeTeamGoals = homeGoals;
            AwayTeamGoals = awayGoals;
            UpdateTimestamp();
        }
    }
}
