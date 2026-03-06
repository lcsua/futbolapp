using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Matches.AddMatchIncident;

public interface IAddMatchIncidentUseCase
{
    Task<AddMatchIncidentResponse> ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, AddMatchIncidentRequest request, CancellationToken cancellationToken = default);
}
