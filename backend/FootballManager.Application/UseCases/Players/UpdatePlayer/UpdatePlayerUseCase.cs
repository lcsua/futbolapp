using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Players.UpdatePlayer;

public sealed class UpdatePlayerUseCase : IUpdatePlayerUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePlayerUseCase(
        IUserLeagueRepository userLeagueRepository,
        ITeamRepository teamRepository,
        IPlayerRepository playerRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(UpdatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team == null || team.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Team {request.TeamId} not found in league {request.LeagueId}.");

        var player = await _playerRepository.GetByIdAsync(request.PlayerId, cancellationToken);
        if (player == null || player.TeamId != request.TeamId)
            throw new KeyNotFoundException($"Player {request.PlayerId} not found in team {request.TeamId}.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new BusinessException("Nombre y apellido son obligatorios.");

        player.UpdateIdentity(
            request.FirstName,
            request.LastName,
            request.Nickname,
            request.Document,
            request.BirthDate);

        var position = PlayerMapping.ParsePosition(request.Position);
        player.UpdateDetails(request.JerseyNumber ?? player.JerseyNumber, position, player.HeightCm, player.WeightKg);

        if (request.IsActive.HasValue)
            player.SetActive(request.IsActive.Value);

        _playerRepository.Update(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
