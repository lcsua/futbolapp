using System;
using System.Collections.Generic;

namespace FootballManager.Application.UseCases.Matches.ImportMatchResults
{
    public class ImportMatchResultsRequest
    {
        public Guid LeagueId { get; set; }
        public Guid SeasonId { get; set; }
        public Guid UserId { get; set; }
        public List<ImportMatchResultsDivisionDto> Divisions { get; set; } = new();
    }

    public class ImportMatchResultsDivisionDto
    {
        public Guid DivisionId { get; set; }
        public List<ImportMatchResultItemDto> Matches { get; set; } = new();
    }

    public class ImportMatchResultItemDto
    {
        public Guid HomeTeamId { get; set; }
        public Guid AwayTeamId { get; set; }
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        /// <summary>JSON status e.g. finished, suspended.</summary>
        public string? Status { get; set; }
    }

    public class ImportMatchResultsResponse
    {
        public int UpdatedCount { get; }
        public int CreatedCount { get; }
        public List<string> Warnings { get; }

        public ImportMatchResultsResponse(int updatedCount, int createdCount, List<string>? warnings = null)
        {
            UpdatedCount = updatedCount;
            CreatedCount = createdCount;
            Warnings = warnings ?? new List<string>();
        }
    }

    public interface IImportMatchResultsUseCase
    {
        System.Threading.Tasks.Task<ImportMatchResultsResponse> ExecuteAsync(
            ImportMatchResultsRequest request,
            System.Threading.CancellationToken cancellationToken = default);
    }
}
