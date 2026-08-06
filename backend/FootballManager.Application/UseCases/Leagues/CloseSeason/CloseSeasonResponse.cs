namespace FootballManager.Application.UseCases.Leagues.CloseSeason;

public sealed class CloseSeasonResponse
{
    public int PendingResultsCount { get; }

    public CloseSeasonResponse(int pendingResultsCount)
    {
        PendingResultsCount = pendingResultsCount;
    }
}
