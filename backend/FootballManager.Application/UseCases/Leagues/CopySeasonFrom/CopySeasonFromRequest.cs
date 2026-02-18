namespace FootballManager.Application.UseCases.Leagues.CopySeasonFrom
{
    public class CopySeasonFromRequest
    {
        public Guid LeagueId { get; }
        public Guid SeasonId { get; }
        public Guid SourceSeasonId { get; }
        public Guid UserId { get; }

        public CopySeasonFromRequest(Guid leagueId, Guid seasonId, Guid sourceSeasonId, Guid userId)
        {
            LeagueId = leagueId;
            SeasonId = seasonId;
            SourceSeasonId = sourceSeasonId;
            UserId = userId;
        }
    }
}
