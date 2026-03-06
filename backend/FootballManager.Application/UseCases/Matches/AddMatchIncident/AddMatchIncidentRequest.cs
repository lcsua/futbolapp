using System;

namespace FootballManager.Application.UseCases.Matches.AddMatchIncident;

public sealed class AddMatchIncidentRequest
{
    public int Minute { get; set; }
    public Guid? TeamId { get; set; }
    public string PlayerName { get; set; }
    public string IncidentType { get; set; }
    public string Notes { get; set; }
}
