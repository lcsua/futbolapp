using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.DeleteDivisionRound;

public interface IDeleteDivisionRoundUseCase
{
    Task<DeleteDivisionRoundResponse> ExecuteAsync(DeleteDivisionRoundRequest request, CancellationToken cancellationToken = default);
}

public class DeleteDivisionRoundTarget
{
    public Guid DivisionId { get; set; }
    public List<int> Rounds { get; set; } = new();
}

public class DeleteDivisionRoundRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public Guid SeasonId { get; set; }
    public string? Confirmation { get; set; }
    public List<DeleteDivisionRoundTarget> Targets { get; set; } = new();
}

public class DeleteDivisionRoundResponse
{
    public int DeletedCount { get; }
    public int ResultsRemovedCount { get; }

    public DeleteDivisionRoundResponse(int deletedCount, int resultsRemovedCount)
    {
        DeletedCount = deletedCount;
        ResultsRemovedCount = resultsRemovedCount;
    }
}

public static class DeleteDivisionRoundConfirmation
{
    public const string Word = "ELIMINAR";

    public static bool IsValid(string? confirmation) =>
        string.Equals(confirmation?.Trim(), Word, StringComparison.Ordinal);
}
