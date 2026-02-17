using System;
using System.Collections.Generic;
using FootballManager.Domain.Common;
using FootballManager.Domain.Enums;

namespace FootballManager.Domain.Entities
{
    public class Fixture : Entity
    {
        public Guid LeagueId { get; private set; }
        public virtual League League { get; private set; }

        public Guid SeasonId { get; private set; }
        public virtual Season Season { get; private set; }

        public Guid DivisionSeasonId { get; private set; }
        public virtual DivisionSeason DivisionSeason { get; private set; }

        public Guid HomeTeamDivisionSeasonId { get; private set; }
        public virtual TeamDivisionSeason HomeTeamDivisionSeason { get; private set; }

        public Guid AwayTeamDivisionSeasonId { get; private set; }
        public virtual TeamDivisionSeason AwayTeamDivisionSeason { get; private set; }

        public int RoundNumber { get; private set; }
        public DateOnly MatchDate { get; private set; }
        public TimeOnly StartTime { get; private set; }
        public Guid FieldId { get; private set; }
        public virtual Field Field { get; private set; }
        public MatchStatus Status { get; private set; }

        public string RefereeName { get; private set; }
        public int? Attendance { get; private set; }

        public virtual Result Result { get; private set; }

        private readonly List<MatchEvent> _events = new();
        public virtual IReadOnlyCollection<MatchEvent> Events => _events.AsReadOnly();

        protected Fixture() { }

        public Fixture(League league, Season season, DivisionSeason divisionSeason, TeamDivisionSeason homeTeamDivisionSeason, TeamDivisionSeason awayTeamDivisionSeason, int roundNumber, DateOnly matchDate, TimeOnly startTime, Field field)
        {
            League = league ?? throw new ArgumentNullException(nameof(league));
            LeagueId = league.Id;
            Season = season ?? throw new ArgumentNullException(nameof(season));
            SeasonId = season.Id;
            DivisionSeason = divisionSeason ?? throw new ArgumentNullException(nameof(divisionSeason));
            DivisionSeasonId = divisionSeason.Id;
            HomeTeamDivisionSeason = homeTeamDivisionSeason ?? throw new ArgumentNullException(nameof(homeTeamDivisionSeason));
            HomeTeamDivisionSeasonId = homeTeamDivisionSeason.Id;
            AwayTeamDivisionSeason = awayTeamDivisionSeason ?? throw new ArgumentNullException(nameof(awayTeamDivisionSeason));
            AwayTeamDivisionSeasonId = awayTeamDivisionSeason.Id;

            if (HomeTeamDivisionSeasonId == AwayTeamDivisionSeasonId)
                throw new ArgumentException("Home and Away team assignments must be different.");

            RoundNumber = roundNumber;
            MatchDate = matchDate;
            StartTime = startTime;
            Field = field ?? throw new ArgumentNullException(nameof(field));
            FieldId = field.Id;
            Status = MatchStatus.SCHEDULED;
            RefereeName = string.Empty;
        }

        public void Schedule(DateOnly date, TimeOnly startTime, Field field)
        {
            MatchDate = date;
            StartTime = startTime;
            Field = field ?? throw new ArgumentNullException(nameof(field));
            FieldId = field.Id;
            UpdateTimestamp();
        }

        public void SetReferee(string refereeName)
        {
            RefereeName = refereeName ?? string.Empty;
            UpdateTimestamp();
        }

        public void ChangeStatus(MatchStatus status)
        {
            Status = status;
            UpdateTimestamp();
        }

        public void SetResult(Result result)
        {
            Result = result;
            UpdateTimestamp();
        }
    }
}
