using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.ShiftFixtureDates;

public sealed class ShiftFixtureDatesResponse
{
    public int UpdatedCount { get; }
    public int SkippedCount { get; }
    public IReadOnlyList<string> Errors { get; }

    public ShiftFixtureDatesResponse(int updatedCount, int skippedCount, IReadOnlyList<string> errors)
    {
        UpdatedCount = updatedCount;
        SkippedCount = skippedCount;
        Errors = errors ?? Array.Empty<string>();
    }

    public static ShiftFixtureDatesResponse WithErrors(IReadOnlyList<string> errors) =>
        new(0, 0, errors);
}
