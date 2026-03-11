namespace FootballManager.Application.UseCases.Seasons.GetStandings;

public sealed class GetStandingsResponse
{
    public IReadOnlyList<DivisionStandingsDto> Divisions { get; }

    public GetStandingsResponse(IReadOnlyList<DivisionStandingsDto> divisions)
    {
        Divisions = divisions;
    }
}

public sealed class DivisionStandingsDto
{
    public Guid DivisionId { get; }
    public string DivisionName { get; }
    public IReadOnlyList<TeamStandingDto> Standings { get; }

    public DivisionStandingsDto(Guid divisionId, string divisionName, IReadOnlyList<TeamStandingDto> standings)
    {
        DivisionId = divisionId;
        DivisionName = divisionName;
        Standings = standings;
    }
}

public sealed class TeamStandingDto
{
    public int Position { get; }
    public Guid TeamId { get; }
    public string TeamName { get; }
    public int Points { get; }
    public int Played { get; }
    public int Wins { get; }
    public int Draws { get; }
    public int Losses { get; }
    public int GoalsFor { get; }
    public int GoalsAgainst { get; }
    public int GoalDifference { get; }

    public TeamStandingDto(int position, Guid teamId, string teamName, int points, int played, int wins, int draws, int losses, int goalsFor, int goalsAgainst, int goalDifference)
    {
        Position = position;
        TeamId = teamId;
        TeamName = teamName;
        Points = points;
        Played = played;
        Wins = wins;
        Draws = draws;
        Losses = losses;
        GoalsFor = goalsFor;
        GoalsAgainst = goalsAgainst;
        GoalDifference = goalDifference;
    }
}
