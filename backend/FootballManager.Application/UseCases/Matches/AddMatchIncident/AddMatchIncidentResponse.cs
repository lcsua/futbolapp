using System;

namespace FootballManager.Application.UseCases.Matches.AddMatchIncident;

public sealed class AddMatchIncidentResponse
{
    public Guid Id { get; }

    public AddMatchIncidentResponse(Guid id)
    {
        Id = id;
    }
}
