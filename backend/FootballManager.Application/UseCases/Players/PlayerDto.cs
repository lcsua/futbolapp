using System;

namespace FootballManager.Application.UseCases.Players;

public sealed class PlayerDto
{
    public Guid Id { get; set; }
    public Guid TeamId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string? Position { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool IsActive { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
