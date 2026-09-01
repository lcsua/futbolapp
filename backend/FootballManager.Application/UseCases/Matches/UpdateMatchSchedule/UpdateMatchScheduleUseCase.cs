using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchSchedule;

public sealed class UpdateMatchScheduleUseCase : IUpdateMatchScheduleUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMatchScheduleUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IFieldRepository fieldRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(
        Guid leagueId,
        Guid matchId,
        Guid userId,
        UpdateMatchScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(matchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Match {matchId} not found.");
        if (fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        SeasonGuard.EnsureOpen(fixture.Season);

        if (!TryParseTime(request.StartTime, out var startTime))
            throw new BusinessException("Horario inválido.");

        var field = await _fieldRepository.GetByIdAsync(request.FieldId, cancellationToken)
            ?? throw new KeyNotFoundException($"Field {request.FieldId} not found.");
        if (field.LeagueId != leagueId)
            throw new ForbiddenAccessException("Field does not belong to this league.");

        fixture.AssignKickoffAndField(startTime, field);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static bool TryParseTime(string? raw, out TimeOnly time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var s = raw.Trim().Replace('.', ':');
        string[] formats = { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss" };
        if (TimeOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
            return true;

        return TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
    }
}
