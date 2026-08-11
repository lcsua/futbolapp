using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.ImportMatchSchedule;

public interface IImportMatchScheduleUseCase
{
    Task<ImportMatchScheduleResponse> ExecuteAsync(
        ImportMatchScheduleRequest request,
        CancellationToken cancellationToken = default);
}

public class ImportMatchScheduleRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid DivisionId { get; set; }
    public int Round { get; set; }
    public List<ImportMatchScheduleRowDto> Rows { get; set; } = new();
}

public class ImportMatchScheduleRowDto
{
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    /// <summary>Kickoff as HH:mm or HH:mm:ss.</summary>
    public string? StartTime { get; set; }
    /// <summary>Exact field name in the league (e.g. A, B, SANTA ROSA).</summary>
    public string? FieldName { get; set; }
    /// <summary>
    /// If true, allow applying when fixture has home/away inverted vs CSV.
    /// </summary>
    public bool AllowInverted { get; set; }
}

public class ImportMatchScheduleResponse
{
    public int UpdatedCount { get; }
    public IReadOnlyList<string> Warnings { get; }

    public ImportMatchScheduleResponse(int updatedCount, IReadOnlyList<string> warnings)
    {
        UpdatedCount = updatedCount;
        Warnings = warnings;
    }
}
