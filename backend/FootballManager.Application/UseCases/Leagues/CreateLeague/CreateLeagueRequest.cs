using System;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public class CreateLeagueRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsPublic { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid UserId { get; set; }

        public CreateLeagueRequest(string name, string country, string? slug = null, string? description = null, string? logoUrl = null, bool isPublic = false, bool isActive = true, Guid userId = default)
        {
            Name = name;
            Country = country;
            Slug = slug;
            Description = description;
            LogoUrl = logoUrl;
            IsPublic = isPublic;
            IsActive = isActive;
            UserId = userId;
        }
        
        // Parameterless constructor for deserialization
        public CreateLeagueRequest() { }
    }
}
