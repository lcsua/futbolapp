using System;

namespace FootballManager.Domain.Entities
{
    public class Standing
    {
        public Guid TeamId { get; private set; }
        public string TeamName { get; private set; }
        public string LogoUrl { get; private set; }
        public string DivisionName { get; private set; }
        public string SeasonName { get; private set; }
        public string LeagueName { get; private set; }
        public long MatchesPlayed { get; private set; }
        public long Wins { get; private set; }
        public long Draws { get; private set; }
        public long Losses { get; private set; }
        public long Points { get; private set; }
        public long GoalsFor { get; private set; }
        public long GoalsAgainst { get; private set; }
        public long GoalDifference { get; private set; }
    }
}
