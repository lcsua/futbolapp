using System;

namespace FootballManager.Application.UseCases.Leagues.UpdateField
{
    public class UpdateFieldRequest
    {
        public Guid LeagueId { get; set; }
        public Guid FieldId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public double? GeoLat { get; set; }
        public double? GeoLng { get; set; }
        public bool IsAvailable { get; set; }
        public string? Description { get; set; }
    }
}
