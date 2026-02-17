using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities
{
    public class Season : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public string Name { get; private set; }
        public DateOnly StartDate { get; private set; }
        public DateOnly? EndDate { get; private set; }
        public bool IsActive { get; private set; }

        private readonly List<DivisionSeason> _divisionSeasons = new();
        public virtual IReadOnlyCollection<DivisionSeason> DivisionSeasons => _divisionSeasons.AsReadOnly();

        protected Season() { }

        public Season(League league, string name, DateOnly startDate, DateOnly? endDate)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Season name cannot be empty.", nameof(name));
            StartDate = startDate;
            EndDate = endDate;
            IsActive = true;
        }

        public void UpdateDetails(string name, DateOnly startDate, DateOnly? endDate)
        {
            Name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Season name cannot be empty.", nameof(name));
            StartDate = startDate;
            EndDate = endDate;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }
    }
}
