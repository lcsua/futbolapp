namespace FootballManager.Application.UseCases.Seasons.GetStandings;

public sealed class GetStandingsRequest
{
    public Guid LeagueId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
}
