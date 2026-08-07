using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Leagues.AssignFixtureDates;

public sealed class AssignFixtureDatesResponse
{
    public int UpdatedCount { get; }
    public int RoundCount { get; }
    public IReadOnlyList<string> Errors { get; }

    public AssignFixtureDatesResponse(int updatedCount, int roundCount, IReadOnlyList<string> errors)
    {
        UpdatedCount = updatedCount;
        RoundCount = roundCount;
        Errors = errors ?? Array.Empty<string>();
    }

    public static AssignFixtureDatesResponse WithErrors(IReadOnlyList<string> errors) =>
        new(0, 0, errors);
}
