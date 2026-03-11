namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public sealed class PreviewFixtureImportResponse
{
    public string ImportType { get; }
    public IReadOnlyList<PreviewFixtureRowDto> Rows { get; }
    public IReadOnlyList<string> Errors { get; }

    public PreviewFixtureImportResponse(string importType, IReadOnlyList<PreviewFixtureRowDto> rows, IReadOnlyList<string> errors)
    {
        ImportType = importType;
        Rows = rows;
        Errors = errors;
    }
}

public sealed class PreviewFixtureRowDto
{
    public int Round { get; }
    public string? Date { get; }
    public string? Time { get; }
    public string? Field { get; }
    public string HomeTeam { get; }
    public string AwayTeam { get; }
    public string? RowError { get; }

    public PreviewFixtureRowDto(int round, string? date, string? time, string? field, string homeTeam, string awayTeam, string? rowError = null)
    {
        Round = round;
        Date = date;
        Time = time;
        Field = field;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        RowError = rowError;
    }
}
