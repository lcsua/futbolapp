using System;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Players;

internal static class PlayerMapping
{
    public static PlayerDto ToDto(Player player) => new()
    {
        Id = player.Id,
        TeamId = player.TeamId,
        FirstName = player.FirstName,
        LastName = player.LastName,
        Nickname = player.Nickname ?? string.Empty,
        Document = player.Document ?? string.Empty,
        Position = player.Position?.ToString(),
        BirthDate = player.BirthDate,
        IsActive = player.IsActive,
        DisplayName = player.DisplayName
    };

    public static PlayerPosition? ParsePosition(string? position)
    {
        if (string.IsNullOrWhiteSpace(position))
            return null;
        return Enum.TryParse<PlayerPosition>(position.Trim(), true, out var p) ? p : null;
    }
}
