using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteDivision;

public sealed class DeleteDivisionUseCase : IDeleteDivisionUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IDivisionRepository _divisionRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDivisionUseCase(
        IUserLeagueRepository userLeagueRepository,
        IDivisionRepository divisionRepository,
        IFixtureRepository fixtureRepository,
        ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(DeleteDivisionRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken);
        if (division == null)
            throw new KeyNotFoundException($"Division {request.DivisionId} not found.");
        if (division.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Division does not belong to this league.");

        // Leaf → root. Affects every season where this division is assigned.
        await _fixtureRepository.RemoveByDivisionIdAsync(request.DivisionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _teamDivisionSeasonRepository.RemoveByDivisionIdAsync(request.DivisionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cascades DivisionSeasonField + DivisionMatchRules.
        await _divisionSeasonRepository.RemoveByDivisionIdAsync(request.DivisionId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _divisionRepository.RemoveByIdAsync(request.DivisionId, cancellationToken);
    }
}
