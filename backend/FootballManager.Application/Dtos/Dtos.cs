using System;

namespace FootballManager.Application.Dtos
{
    public record LeagueDto(Guid Id, string Name, string Country, string Description, string LogoUrl);
    public record SeasonDto(Guid Id, string Name, DateOnly StartDate, DateOnly? EndDate);
    public record TeamDto(Guid Id, string Name, string ShortName, string LogoUrl, string? Email, int? FoundedYear, string DelegateName, string DelegateContact, string PhotoUrl);
    public record DivisionDto(Guid Id, Guid LeagueId, string Name, string? Description);
    public record FieldDto(Guid Id, string Name, string? Address, string? City, double? GeoLat, double? GeoLng, bool IsAvailable, string? Description);
}
