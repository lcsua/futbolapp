using System;

namespace FootballManager.Application.Dtos
{
    public record LeagueDto(Guid Id, string Name, string Slug, string Country, string Description, string LogoUrl, bool IsPublic, bool IsActive);
    public record SeasonDto(Guid Id, string Name, DateOnly StartDate, DateOnly? EndDate);
    public record ClubDto(Guid Id, string Name, string LogoUrl);
    public record TeamDto(
        Guid Id,
        string Name,
        string? Suffix,
        string DisplayName,
        string ShortName,
        string LogoUrl,
        string? Email,
        int? FoundedYear,
        string DelegateName,
        string DelegateContact,
        string PhotoUrl,
        Guid? ClubId,
        string? ClubName);
    public record DivisionDto(
        Guid Id,
        Guid LeagueId,
        string Name,
        string? Description,
        bool KickoffRestrictionEnabled,
        TimeOnly? KickoffRestrictionStart,
        TimeOnly? KickoffRestrictionEnd);
    public record FieldDto(Guid Id, string Name, string? Address, string? City, double? GeoLat, double? GeoLng, bool IsAvailable, string? Description);
}
