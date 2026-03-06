using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Matches.DeleteMatchIncident;

public sealed class DeleteMatchIncidentUseCase : IDeleteMatchIncidentUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IMatchIncidentRepository _incidentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMatchIncidentUseCase(
        IUserLeagueRepository userLeagueRepository,
        IMatchIncidentRepository incidentRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _incidentRepository = incidentRepository ?? throw new ArgumentNullException(nameof(incidentRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(Guid leagueId, Guid incidentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var incident = await _incidentRepository.GetByIdAsync(incidentId, cancellationToken);
        if (incident == null)
            throw new KeyNotFoundException($"Incident {incidentId} not found.");
        if (incident.Fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Incident does not belong to this league.");

        await _incidentRepository.DeleteAsync(incidentId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
