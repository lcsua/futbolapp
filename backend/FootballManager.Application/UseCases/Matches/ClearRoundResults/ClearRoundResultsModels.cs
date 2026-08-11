using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.ClearRoundResults;

public interface IClearRoundResultsUseCase
{
    Task<ClearRoundResultsResponse> ExecuteAsync(ClearRoundResultsRequest request, CancellationToken cancellationToken = default);
}

public class ClearRoundResultsRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid DivisionId { get; set; }
    public int Round { get; set; }
}

public class ClearRoundResultsResponse
{
    public int ClearedCount { get; }

    public ClearRoundResultsResponse(int clearedCount)
    {
        ClearedCount = clearedCount;
    }
}
