using System.Globalization;
using System.Text;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public sealed class PreviewFixtureImportUseCase : IPreviewFixtureImportUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFieldRepository _fieldRepository;

    public PreviewFixtureImportUseCase(
        IUserLeagueRepository userLeagueRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFieldRepository fieldRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
    }

    public async Task<PreviewFixtureImportResponse> ExecuteAsync(PreviewFixtureImportRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new KeyNotFoundException("Division is not assigned to this season.");

        var (parsedRows, parseErrors, importType) = ParseCsv(request.CsvText);
        if (parseErrors.Count > 0)
            return new PreviewFixtureImportResponse("", new List<PreviewFixtureRowDto>(), parseErrors);

        var fields = await _fieldRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var divisionName = divisionSeason.Division.Name;
        var teamAssignments = divisionSeason.TeamAssignments.ToList();
        var previewRows = new List<PreviewFixtureRowDto>();
        var rowErrors = new List<string>();

        for (var i = 0; i < parsedRows.Count; i++)
        {
            var row = parsedRows[i];
            var rowNum = i + 1;
            string? rowError = null;

            if (string.Equals(row.HomeTeam.Trim(), row.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase))
                rowError = "Home and away team cannot be the same.";
            else
            {
                var homeTds = teamAssignments.FirstOrDefault(ta => string.Equals(ta.Team.Name.Trim(), row.HomeTeam.Trim(), StringComparison.OrdinalIgnoreCase));
                var awayTds = teamAssignments.FirstOrDefault(ta => string.Equals(ta.Team.Name.Trim(), row.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase));
                if (homeTds == null) rowError = $"Team '{row.HomeTeam.Trim()}' does not belong to division {divisionName}.";
                else if (awayTds == null) rowError = $"Team '{row.AwayTeam.Trim()}' does not belong to division {divisionName}.";
            }

            if (rowError == null && !string.IsNullOrWhiteSpace(row.Date) && !DateOnly.TryParse(row.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                rowError = "Invalid date format. Use ISO (e.g. 2026-03-01).";
            if (rowError == null && !string.IsNullOrWhiteSpace(row.Time) && !TimeOnly.TryParse(row.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                rowError = "Invalid time format.";
            if (rowError == null && importType == "Full" && !string.IsNullOrWhiteSpace(row.Field))
            {
                var field = fields.FirstOrDefault(f => string.Equals(f.Name.Trim(), row.Field!.Trim(), StringComparison.OrdinalIgnoreCase));
                if (field == null) rowError = $"Field '{row.Field!.Trim()}' not found in league.";
            }

            if (rowError != null) rowErrors.Add($"Row {rowNum}: {rowError}");
            previewRows.Add(new PreviewFixtureRowDto(row.Round, row.Date, row.Time, row.Field, row.HomeTeam, row.AwayTeam, rowError));
        }

        return new PreviewFixtureImportResponse(importType, previewRows, rowErrors);
    }

    private static (List<ParsedFixtureRow> rows, List<string> errors, string importType) ParseCsv(string csvText)
    {
        var errors = new List<string>();
        var lines = csvText
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            errors.Add("CSV text is empty.");
            return (new List<ParsedFixtureRow>(), errors, "");
        }

        var dataLines = new List<string[]>();
        foreach (var line in lines)
        {
            var cells = SplitCsvLine(line).Select(c => c.Trim()).ToArray();
            if (cells.Length == 0) continue;
            if (cells.Length != 3 && cells.Length != 4 && cells.Length != 6)
            {
                errors.Add($"Unsupported column count: {cells.Length}. Use 3, 4, or 6 columns.");
                return (new List<ParsedFixtureRow>(), errors, "");
            }
            if (!int.TryParse(cells[0], NumberStyles.None, CultureInfo.InvariantCulture, out _) && dataLines.Count == 0)
                continue;
            dataLines.Add(cells);
        }

        if (dataLines.Count == 0)
        {
            errors.Add("No data rows found.");
            return (new List<ParsedFixtureRow>(), errors, "");
        }

        var colCount = dataLines[0].Length;
        string importType = colCount switch { 3 => "Simple", 4 => "WithDate", 6 => "Full", _ => "" };
        var rows = new List<ParsedFixtureRow>();
        for (var i = 0; i < dataLines.Count; i++)
        {
            var cells = dataLines[i];
            if (cells.Length != colCount)
            {
                errors.Add($"Row {i + 1}: expected {colCount} columns, got {cells.Length}.");
                continue;
            }
            if (!int.TryParse(cells[0], NumberStyles.None, CultureInfo.InvariantCulture, out var round) || round < 1)
            {
                errors.Add($"Row {i + 1}: invalid round number.");
                continue;
            }
            string? dateStr = null, timeStr = null, fieldStr = null;
            string homeTeam, awayTeam;
            if (colCount == 3)
            {
                homeTeam = cells[1];
                awayTeam = cells[2];
            }
            else if (colCount == 4)
            {
                dateStr = cells[1];
                homeTeam = cells[2];
                awayTeam = cells[3];
            }
            else
            {
                dateStr = cells[1];
                timeStr = cells[2];
                fieldStr = cells[3];
                homeTeam = cells[4];
                awayTeam = cells[5];
            }
            rows.Add(new ParsedFixtureRow(round, dateStr, timeStr, fieldStr, homeTeam, awayTeam));
        }
        return (rows, errors, importType);
    }

    private static IEnumerable<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
                inQuotes = !inQuotes;
            else if ((c == ',' && !inQuotes) || c == '\n' || c == '\r')
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(c);
        }
        result.Add(current.ToString());
        return result;
    }

    private sealed record ParsedFixtureRow(int Round, string? Date, string? Time, string? Field, string HomeTeam, string AwayTeam);
}
