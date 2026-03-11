namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public sealed class ImportFixturesRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid DivisionId { get; set; }
    public string CsvText { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
