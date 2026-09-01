using System;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchSchedule;

public sealed class UpdateMatchScheduleRequest
{
    public string StartTime { get; set; } = string.Empty;
    public Guid FieldId { get; set; }
}
