using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonSetup
{
    public class SeasonSetupDivisionDto
    {
        public Guid DivisionId { get; }
        public string DivisionName { get; }
        public List<TeamDto> Teams { get; }

        public SeasonSetupDivisionDto(Guid divisionId, string divisionName, List<TeamDto> teams)
        {
            DivisionId = divisionId;
            DivisionName = divisionName;
            Teams = teams ?? new List<TeamDto>();
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
