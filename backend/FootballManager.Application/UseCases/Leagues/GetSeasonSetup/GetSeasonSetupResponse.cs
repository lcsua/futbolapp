using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonSetup
{
    public class SeasonSetupDivisionDto
    {
        public Guid DivisionId { get; }
        public string DivisionName { get; }
        public List<TeamDto> Teams { get; }
        /// <summary>True when this division already has committed fixtures for the season.</summary>
        public bool FixturesLocked { get; }
        /// <summary>Teams of this division that already play a fixture this season; they cannot leave it.</summary>
        public List<Guid> TeamIdsWithFixtures { get; }

        public SeasonSetupDivisionDto(
            Guid divisionId,
            string divisionName,
            List<TeamDto> teams,
            bool fixturesLocked = false,
            List<Guid>? teamIdsWithFixtures = null)
        {
            DivisionId = divisionId;
            DivisionName = divisionName;
            Teams = teams ?? new List<TeamDto>();
            FixturesLocked = fixturesLocked;
            TeamIdsWithFixtures = teamIdsWithFixtures ?? new List<Guid>();
        }
    }

    public class GetSeasonSetupResponse
    {
        public List<TeamDto> UnassignedTeams { get; }
        public List<SeasonSetupDivisionDto> Divisions { get; }

        public GetSeasonSetupResponse(List<TeamDto> unassignedTeams, List<SeasonSetupDivisionDto> divisions)
        {
            UnassignedTeams = unassignedTeams ?? new List<TeamDto>();
            Divisions = divisions ?? new List<SeasonSetupDivisionDto>();
        }
    }
}
