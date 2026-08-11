using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Players.ImportPlayers;

public sealed class ImportPlayersUseCase : IImportPlayersUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportPlayersUseCase(
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

    public async Task<ImportPlayersResponse> ExecuteAsync(ImportPlayersRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
        if (team == null || team.LeagueId != request.LeagueId)
            throw new KeyNotFoundException($"Team {request.TeamId} not found in league {request.LeagueId}.");

        if (request.Players == null || request.Players.Count == 0)
            throw new BusinessException("Debe enviar al menos un jugador para importar.");

        var createdIds = new List<Guid>();
        foreach (var item in request.Players)
        {
            if (string.IsNullOrWhiteSpace(item.FirstName) || string.IsNullOrWhiteSpace(item.LastName))
                throw new BusinessException("Cada jugador importado requiere nombre y apellido.");

            var player = new Player(
                team,
                item.FirstName,
                item.LastName,
                item.Nickname,
                item.Document,
                item.BirthDate,
                PlayerMapping.ParsePosition(item.Position));

            await _playerRepository.AddAsync(player, cancellationToken);
            createdIds.Add(player.Id);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ImportPlayersResponse(createdIds.Count, createdIds);
    }
}
