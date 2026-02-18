namespace FootballManager.Application.UseCases.Leagues.GetSeasonSetup
{
    public class GetSeasonSetupRequest
    {
        public Guid LeagueId { get; }
        public Guid SeasonId { get; }
        public Guid UserId { get; }

        public GetSeasonSetupRequest(Guid leagueId, Guid seasonId, Guid userId)
        {
            LeagueId = leagueId;
            SeasonId = seasonId;
            UserId = userId;
        }
    }
}
