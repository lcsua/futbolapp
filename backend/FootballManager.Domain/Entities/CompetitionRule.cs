using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class CompetitionRule : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public Guid? SeasonId { get; private set; }
        public virtual Season Season { get; private set; }

        public int MatchesPerWeek { get; private set; }
        public bool IsHomeAway { get; private set; }

        private readonly List<CompetitionMatchDay> _matchDays = new();
        public virtual IReadOnlyCollection<CompetitionMatchDay> MatchDays => _matchDays.AsReadOnly();

        protected CompetitionRule() { }

        public CompetitionRule(League league, int matchesPerWeek = 1, bool isHomeAway = false, Season season = null)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            SeasonId = season?.Id;
            Season = season;
            MatchesPerWeek = matchesPerWeek;
            IsHomeAway = isHomeAway;
        }

        public void UpdateDetails(int matchesPerWeek, bool isHomeAway)
        {
            MatchesPerWeek = matchesPerWeek;
            IsHomeAway = isHomeAway;
            UpdateTimestamp();
        }
    }
}
