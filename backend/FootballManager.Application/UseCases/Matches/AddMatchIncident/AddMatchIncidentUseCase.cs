using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.AddMatchIncident;

public sealed class AddMatchIncidentUseCase : IAddMatchIncidentUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IMatchIncidentRepository _incidentRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddMatchIncidentUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IMatchIncidentRepository incidentRepository,
        IPlayerRepository playerRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _incidentRepository = incidentRepository ?? throw new ArgumentNullException(nameof(incidentRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AddMatchIncidentResponse> ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, AddMatchIncidentRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(matchId, cancellationToken);
        if (fixture == null)
            throw new KeyNotFoundException($"Match {matchId} not found.");
        if (fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        if (request.Minute.HasValue && request.Minute.Value < 0)
            throw new BusinessException("Incident minute must be >= 0.");

        Guid? playerId = request.PlayerId;
        var playerName = request.PlayerName?.Trim() ?? string.Empty;
        var teamId = request.TeamId;

        if (playerId.HasValue)
        {
            var player = await _playerRepository.GetByIdAsync(playerId.Value, cancellationToken);
            if (player == null)
                throw new BusinessException("Player not found.");

            var homeTeamId = fixture.HomeTeamDivisionSeason?.TeamId;
            var awayTeamId = fixture.AwayTeamDivisionSeason?.TeamId;
            if (player.TeamId != homeTeamId && player.TeamId != awayTeamId)
                throw new BusinessException("Player does not belong to either team in this match.");

            if (teamId.HasValue && teamId.Value != player.TeamId)
                throw new BusinessException("Selected player does not belong to the selected team.");

            teamId ??= player.TeamId;
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = player.DisplayName;
        }

        var incidentType = ParseIncidentType(request.IncidentType);
        var incident = new MatchIncident(
            fixture,
            request.Minute,
            teamId,
            playerName,
            incidentType,
            request.Notes,
            playerId);

        await _incidentRepository.AddAsync(incident, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AddMatchIncidentResponse(incident.Id);
    }

    private static MatchIncidentType ParseIncidentType(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return MatchIncidentType.Other;
        return Enum.TryParse<MatchIncidentType>(value, true, out var t) ? t : MatchIncidentType.Other;
    }
}
