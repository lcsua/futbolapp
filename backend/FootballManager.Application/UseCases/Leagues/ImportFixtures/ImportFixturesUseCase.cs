using System.Globalization;
using System.Text;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public sealed class ImportFixturesUseCase : IImportFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFieldRepository fieldRepository,
        IFixtureRepository fixtureRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ImportFixturesResponse> ExecuteAsync(ImportFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
        if (league == null)
            throw new KeyNotFoundException($"League {request.LeagueId} not found.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null || season.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found or does not belong to this league.");

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new KeyNotFoundException($"Division is not assigned to this season.");

        var (parsedRows, parseErrors, importType) = ParseCsv(request.CsvText);
        if (parseErrors.Count > 0)
            return ImportFixturesResponse.WithErrors(parseErrors);

        var fields = await _fieldRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var divisionName = divisionSeason.Division.Name;
        var (resolvedRows, validationErrors) = ValidateAndResolve(parsedRows, importType, divisionSeason, fields, divisionName);
        if (validationErrors.Count > 0)
            return ImportFixturesResponse.WithErrors(validationErrors);

        await _fixtureRepository.RemoveByDivisionSeasonIdAsync(divisionSeason.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var row in resolvedRows)
        {
            Fixture fixture;
            if (row.MatchDate.HasValue && row.StartTime.HasValue && row.Field != null)
                fixture = new Fixture(league, season, divisionSeason, row.HomeTds, row.AwayTds, row.Round, row.MatchDate, row.StartTime, row.Field);
            else if (row.MatchDate.HasValue || row.StartTime.HasValue || row.Field != null)
                fixture = new Fixture(league, season, divisionSeason, row.HomeTds, row.AwayTds, row.Round, row.MatchDate, row.StartTime, row.Field);
            else
                fixture = new Fixture(league, season, divisionSeason, row.HomeTds, row.AwayTds, row.Round);
            await _fixtureRepository.AddAsync(fixture, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ImportFixturesResponse.Success(resolvedRows.Count);
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
                errors.Add($"Unsupported column count: {cells.Length}. Use 3 (round,home_team,away_team), 4 (round,date,home_team,away_team), or 6 (round,date,time,field,home_team,away_team).");
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
            if (string.IsNullOrWhiteSpace(cells[colCount - 2]) || string.IsNullOrWhiteSpace(cells[colCount - 1]))
            {
                errors.Add($"Row {i + 1}: home team and away team are required.");
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

    private static (List<ResolvedFixtureRow> rows, List<string> errors) ValidateAndResolve(
        List<ParsedFixtureRow> parsed,
        string importType,
        DivisionSeason divisionSeason,
        List<Field> fields,
        string divisionName)
    {
        var errors = new List<string>();
        var resolved = new List<ResolvedFixtureRow>();
        var teamAssignments = divisionSeason.TeamAssignments.ToList();

        for (var i = 0; i < parsed.Count; i++)
        {
            var row = parsed[i];
            var rowNum = i + 1;

            if (string.Equals(row.HomeTeam.Trim(), row.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Row {rowNum}: Home and away team cannot be the same.");
                continue;
            }

            var homeTds = teamAssignments.FirstOrDefault(ta => string.Equals(ta.Team.Name.Trim(), row.HomeTeam.Trim(), StringComparison.OrdinalIgnoreCase));
            var awayTds = teamAssignments.FirstOrDefault(ta => string.Equals(ta.Team.Name.Trim(), row.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase));

            if (homeTds == null)
            {
                errors.Add($"Team '{row.HomeTeam.Trim()}' does not belong to division {divisionName}.");
                continue;
            }
            if (awayTds == null)
            {
                errors.Add($"Team '{row.AwayTeam.Trim()}' does not belong to division {divisionName}.");
                continue;
            }

            DateOnly? matchDate = null;
            if (!string.IsNullOrWhiteSpace(row.Date))
            {
                if (!DateOnly.TryParse(row.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                {
                    errors.Add($"Row {rowNum}: Invalid date format. Use ISO format (e.g. 2026-03-01).");
                    continue;
                }
                matchDate = d;
            }

            TimeOnly? startTime = null;
            if (!string.IsNullOrWhiteSpace(row.Time))
            {
                if (!TimeOnly.TryParse(row.Time, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                {
                    errors.Add($"Row {rowNum}: Invalid time format.");
                    continue;
                }
                startTime = t;
            }

            Field? field = null;
            if (importType == "Full" && !string.IsNullOrWhiteSpace(row.Field))
            {
                field = fields.FirstOrDefault(f => string.Equals(f.Name.Trim(), row.Field!.Trim(), StringComparison.OrdinalIgnoreCase));
                if (field == null)
                {
                    errors.Add($"Row {rowNum}: Field '{row.Field!.Trim()}' not found in league.");
                    continue;
                }
            }

            resolved.Add(new ResolvedFixtureRow(row.Round, matchDate, startTime, field, homeTds, awayTds));
        }

        return (resolved, errors);
    }

    private sealed record ParsedFixtureRow(int Round, string? Date, string? Time, string? Field, string HomeTeam, string AwayTeam);
    private sealed record ResolvedFixtureRow(int Round, DateOnly? MatchDate, TimeOnly? StartTime, Field? Field, TeamDivisionSeason HomeTds, TeamDivisionSeason AwayTds);
}
