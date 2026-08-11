namespace FootballManager.Application.UseCases.Leagues.SeedLeagueDocumentDefaults
{
    public class SeedLeagueDocumentDefaultsResponse
    {
        public int CreatedCount { get; }

        public SeedLeagueDocumentDefaultsResponse(int createdCount)
        {
            CreatedCount = createdCount;
        }
    }
}
