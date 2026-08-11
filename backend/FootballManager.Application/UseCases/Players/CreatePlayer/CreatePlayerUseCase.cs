using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Players.CreatePlayer;

public sealed class CreatePlayerUseCase : ICreatePlayerUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePlayerUseCase(
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

    public async Task<CreatePlayerResponse> ExecuteAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team == null || team.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Team {request.TeamId} not found in league {request.LeagueId}.");

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            throw new BusinessException("Nombre y apellido son obligatorios.");

        var position = PlayerMapping.ParsePosition(request.Position);
        var player = new Player(
            team,
            request.FirstName,
            request.LastName,
            request.Nickname,
            request.Document,
            request.BirthDate,
            position);

        await _playerRepository.AddAsync(player, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreatePlayerResponse(player.Id);
    }
}
