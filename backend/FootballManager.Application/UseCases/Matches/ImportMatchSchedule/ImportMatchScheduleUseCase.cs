using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Matches.ImportMatchSchedule;

public sealed class ImportMatchScheduleUseCase : IImportMatchScheduleUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionRepository _divisionRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly ITeamNameAliasService _aliasService;
    private readonly IUnitOfWork _unitOfWork;

    public ImportMatchScheduleUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionRepository divisionRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IFieldRepository fieldRepository,
        ITeamNameAliasService aliasService,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _aliasService = aliasService ?? throw new ArgumentNullException(nameof(aliasService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ImportMatchScheduleResponse> ExecuteAsync(
        ImportMatchScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        if (request.Round < 1)
            throw new BusinessException("La fecha (round) debe ser mayor o igual a 1.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken)
            ?? throw new KeyNotFoundException($"League {request.LeagueId} not found.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken)
            ?? throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        SeasonGuard.EnsureOpen(season);

        var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Division {request.DivisionId} not found.");
        if (division.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Division does not belong to this league.");

        var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(
            request.SeasonId, request.DivisionId, cancellationToken);
        if (divisionSeason == null)
            throw new BusinessException($"La división \"{division.Name}\" no tiene equipos asignados en esta temporada.");

        var tdsByTeamId = divisionSeason.TeamAssignments
            .GroupBy(ta => ta.TeamId)
            .ToDictionary(g => g.Key, g => g.First());

        var fields = await _fieldRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var fieldByName = fields
            .GroupBy(f => f.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var fixtures = await _fixtureRepository.GetBySeasonAndDivisionAndRoundAsync(
            request.SeasonId, divisionSeason.Id, request.Round, cancellationToken);

        var fixtureByPair = new Dictionary<(Guid HomeTeamId, Guid AwayTeamId), Fixture>();
        foreach (var f in fixtures)
        {
            var homeTeamId = f.HomeTeamDivisionSeason?.TeamId
                ?? throw new BusinessException($"Fixture {f.Id} without home team.");
            var awayTeamId = f.AwayTeamDivisionSeason?.TeamId
                ?? throw new BusinessException($"Fixture {f.Id} without away team.");
            fixtureByPair[(homeTeamId, awayTeamId)] = f;
        }

        var updated = 0;
        var warnings = new List<string>();
        var seenFixtureIds = new HashSet<Guid>();
        var learned = new HashSet<(Guid TeamId, string Normalized)>();

        foreach (var row in request.Rows ?? new List<ImportMatchScheduleRowDto>())
        {
            if (!tdsByTeamId.ContainsKey(row.HomeTeamId) || !tdsByTeamId.ContainsKey(row.AwayTeamId))
            {
                warnings.Add($"Equipos no pertenecientes a {division.Name}: {row.HomeTeamId} / {row.AwayTeamId}.");
                continue;
            }

            if (!TryParseTime(row.StartTime, out var startTime))
            {
                warnings.Add($"Horario inválido \"{row.StartTime}\" para un partido de la fecha {request.Round}.");
                continue;
            }

            Field? field = null;
            if (!string.IsNullOrWhiteSpace(row.FieldName))
            {
                if (!fieldByName.TryGetValue(row.FieldName.Trim(), out field))
                {
                    warnings.Add($"Cancha \"{row.FieldName.Trim()}\" no encontrada en la liga (nombre exacto).");
                    continue;
                }
            }

            Fixture? fixture = null;
            var inverted = false;
            if (fixtureByPair.TryGetValue((row.HomeTeamId, row.AwayTeamId), out var direct))
            {
                fixture = direct;
            }
            else if (fixtureByPair.TryGetValue((row.AwayTeamId, row.HomeTeamId), out var swapped))
            {
                fixture = swapped;
                inverted = true;
            }

            if (fixture == null)
            {
                warnings.Add(
                    $"Sin fixture en {division.Name} fecha {request.Round} para el par indicado (posiblemente otra división).");
                continue;
            }

            if (inverted && !row.AllowInverted)
            {
                warnings.Add(
                    $"Localía invertida (fecha {request.Round}): se omitió hasta confirmar. Fixture {fixture.Id}.");
                continue;
            }

            fixture.AssignKickoffAndField(startTime, field);
            if (!seenFixtureIds.Add(fixture.Id))
            {
                warnings.Add($"Fixture {fixture.Id} aparece más de una vez en el CSV; se usa la última fila válida.");
            }
            else
            {
                updated++;
            }

            await _aliasService.LearnAsync(league, row.HomeTeamId, row.HomeCsvName, "schedule-import", learned, cancellationToken);
            await _aliasService.LearnAsync(league, row.AwayTeamId, row.AwayCsvName, "schedule-import", learned, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ImportMatchScheduleResponse(updated, warnings);
    }

    private static bool TryParseTime(string? raw, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var s = raw.Trim();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*Hs\.?$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        s = s.Replace('.', ':');

        string[] formats = { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss" };
        if (TimeOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
            return true;

        return TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
