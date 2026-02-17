using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class CompetitionMatchDay : Entity
    {
        public Guid CompetitionRuleId { get; private set; }
        public virtual CompetitionRule CompetitionRule { get; private set; }

        public int DayOfWeek { get; private set; }

        protected CompetitionMatchDay() { }

        public CompetitionMatchDay(CompetitionRule competitionRule, int dayOfWeek)
        {
            CompetitionRule = competitionRule ?? throw new ArgumentNullException(nameof(competitionRule));
            CompetitionRuleId = competitionRule.Id;
            if (dayOfWeek < 0 || dayOfWeek > 6)
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Day of week must be 0-6 (Sunday-Saturday).");
            DayOfWeek = dayOfWeek;
        }
    }
}
