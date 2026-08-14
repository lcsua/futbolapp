using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Push;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchResult;

public sealed class UpdateMatchResultUseCase : IUpdateMatchResultUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IMatchIncidentRepository _matchIncidentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPushNotificationService _pushNotifications;

    public UpdateMatchResultUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IResultRepository resultRepository,
        IPlayerRepository playerRepository,
        IMatchIncidentRepository matchIncidentRepository,
        IUnitOfWork unitOfWork,
        IPushNotificationService pushNotifications)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _matchIncidentRepository = matchIncidentRepository ?? throw new ArgumentNullException(nameof(matchIncidentRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _pushNotifications = pushNotifications ?? throw new ArgumentNullException(nameof(pushNotifications));
    }

    public async Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, UpdateMatchResultRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(matchId, cancellationToken);
        if (fixture == null)
            throw new KeyNotFoundException($"Match {matchId} not found.");
        if (fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        var status = ParseStatus(request.Status);
        var countsForStandings = status is MatchStatus.COMPLETED or MatchStatus.PLAYED;

        if (countsForStandings)
        {
            if (request.HomeScore < 0 || request.AwayScore < 0)
                throw new BusinessException("Scores cannot be negative.");

            var existingResult = await _resultRepository.GetByFixtureIdAsync(matchId, cancellationToken);
            if (existingResult != null)
            {
                existingResult.UpdateScore(request.HomeScore, request.AwayScore);
                _resultRepository.Update(existingResult);
            }
            else
            {
                var result = new Result(fixture, request.HomeScore, request.AwayScore);
                await _resultRepository.AddAsync(result, cancellationToken);
            }
        }
        else if (status is MatchStatus.SUSPENDED or MatchStatus.POSTPONED or MatchStatus.CANCELLED or MatchStatus.SCHEDULED)
        {
            var existingResult = await _resultRepository.GetByFixtureIdAsync(matchId, cancellationToken);
            if (existingResult != null)
                _resultRepository.Remove(existingResult);

            await _matchIncidentRepository.DeleteByFixtureAndTypeAsync(fixture.Id, MatchIncidentType.Goal, cancellationToken);
        }

        fixture.ChangeStatus(status);

        if (countsForStandings && request.Goals != null)
            await SyncGoalIncidentsAsync(fixture, request.Goals, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (countsForStandings)
            await TryNotifyResultAsync(fixture, request.HomeScore, request.AwayScore, cancellationToken);
    }

    private async Task TryNotifyResultAsync(Fixture fixture, int homeScore, int awayScore, CancellationToken cancellationToken)
    {
        try
        {
            var home = fixture.HomeTeamDivisionSeason?.Team;
            var away = fixture.AwayTeamDivisionSeason?.Team;
            if (home == null || away == null) return;

            await _pushNotifications.NotifyResultUpdatedAsync(new ResultUpdatedPushEvent
            {
                LeagueId = fixture.LeagueId,
                LeagueSlug = fixture.League?.Slug ?? string.Empty,
                LeagueName = fixture.League?.Name ?? string.Empty,
                FixtureId = fixture.Id,
                RoundNumber = fixture.RoundNumber,
                HomeTeamId = home.Id,
                AwayTeamId = away.Id,
                HomeTeamName = home.DisplayName,
                AwayTeamName = away.DisplayName,
                HomeTeamSlug = home.Slug,
                AwayTeamSlug = away.Slug,
                HomeScore = homeScore,
                AwayScore = awayScore
            }, cancellationToken);
        }
        catch
        {
            // Push must never fail the result update.
        }
    }

    private async Task SyncGoalIncidentsAsync(
        Fixture fixture,
        List<MatchGoalAttributionDto> goals,
        CancellationToken cancellationToken)
    {
        var homeTeamId = fixture.HomeTeamDivisionSeason?.TeamId
            ?? fixture.HomeTeamDivisionSeason?.Team?.Id;
        var awayTeamId = fixture.AwayTeamDivisionSeason?.TeamId
            ?? fixture.AwayTeamDivisionSeason?.Team?.Id;

        if (!homeTeamId.HasValue || !awayTeamId.HasValue)
            throw new BusinessException("No se pudo determinar los equipos del partido.");

        await _matchIncidentRepository.DeleteByFixtureAndTypeAsync(fixture.Id, MatchIncidentType.Goal, cancellationToken);

        foreach (var goal in goals)
        {
            if (goal.TeamId != homeTeamId.Value && goal.TeamId != awayTeamId.Value)
                throw new BusinessException($"El equipo {goal.TeamId} no participa en este partido.");

            var opposingTeamId = goal.TeamId == homeTeamId.Value ? awayTeamId.Value : homeTeamId.Value;
            string playerName = goal.ScorerName?.Trim() ?? string.Empty;

            if (goal.ScorerPlayerId.HasValue)
            {
                var scorer = await _playerRepository.GetByIdAsync(goal.ScorerPlayerId.Value, cancellationToken);
                if (scorer == null || scorer.TeamId != goal.TeamId)
                    throw new BusinessException("El goleador debe pertenecer al equipo que anotó.");
                if (string.IsNullOrWhiteSpace(playerName))
                    playerName = scorer.DisplayName;
            }

            if (goal.AgainstGoalkeeperPlayerId.HasValue)
            {
                var keeper = await _playerRepository.GetByIdAsync(goal.AgainstGoalkeeperPlayerId.Value, cancellationToken);
                if (keeper == null || keeper.TeamId != opposingTeamId)
                    throw new BusinessException("El arquero debe pertenecer al equipo rival.");
            }

            var minute = goal.Minute;
            if (minute.HasValue && minute.Value < 0)
                throw new BusinessException("El minuto del gol no puede ser negativo.");

            var incident = MatchIncident.CreateGoal(
                fixture,
                goal.TeamId,
                minute,
                playerName,
                goal.ScorerPlayerId,
                goal.AgainstGoalkeeperPlayerId);

            await _matchIncidentRepository.AddAsync(incident, cancellationToken);
        }
    }

    private static MatchStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return MatchStatus.SCHEDULED;
        return Enum.TryParse<MatchStatus>(status, true, out var s) ? s : MatchStatus.SCHEDULED;
    }
}
