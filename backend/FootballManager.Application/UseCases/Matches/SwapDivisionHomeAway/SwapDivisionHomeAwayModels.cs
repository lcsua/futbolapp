using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.SwapDivisionHomeAway;

public interface ISwapDivisionHomeAwayUseCase
{
    Task<SwapDivisionHomeAwayResponse> ExecuteAsync(
        SwapDivisionHomeAwayRequest request,
        CancellationToken cancellationToken = default);
}

public class SwapDivisionHomeAwayRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid DivisionId { get; set; }
}

public class SwapDivisionHomeAwayResponse
{
    public int SwappedCount { get; }

    public SwapDivisionHomeAwayResponse(int swappedCount)
    {
        SwappedCount = swappedCount;
    }
}
