namespace FootballManager.Application.UseCases.Leagues.SaveSeasonSetup
{
    public class SaveSeasonSetupDivisionDto
    {
        public Guid DivisionId { get; set; }
        public List<Guid> TeamIds { get; set; } = new();
    }

    public class SaveSeasonSetupRequest
    {
        public Guid LeagueId { get; set; }
        public Guid SeasonId { get; set; }
        public Guid UserId { get; set; }
        public List<SaveSeasonSetupDivisionDto> Divisions { get; set; } = new();
        /// <summary>
        /// Explicit confirmation to change the teams of divisions with committed fixtures: teams may be added,
        /// and removed only while they have no fixture.
        /// </summary>
        public bool AllowChangesToLockedDivisions { get; set; }
    }
}
