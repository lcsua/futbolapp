using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.DeleteMatchIncident;

public interface IDeleteMatchIncidentUseCase
{
    Task ExecuteAsync(Guid leagueId, Guid incidentId, Guid userId, CancellationToken cancellationToken = default);
}
