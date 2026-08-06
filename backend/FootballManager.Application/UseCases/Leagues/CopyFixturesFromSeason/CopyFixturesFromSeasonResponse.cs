using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.CopyFixturesFromSeason;

public sealed class CopyFixturesFromSeasonResponse
{
    public int CopiedCount { get; }
    public IReadOnlyList<string> Errors { get; }

    public CopyFixturesFromSeasonResponse(int copiedCount, IReadOnlyList<string> errors)
    {
        CopiedCount = copiedCount;
        Errors = errors ?? new List<string>();
    }

    public static CopyFixturesFromSeasonResponse WithErrors(IReadOnlyList<string> errors) =>
        new(0, errors);
}
