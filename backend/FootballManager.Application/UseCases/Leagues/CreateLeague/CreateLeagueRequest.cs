using System;

namespace FootballManager.Application.UseCases.Leagues.CreateLeague
{
    public class CreateLeagueRequest
    {
        public string Name { get; set; }
        public string Country { get; set; }
        public string? Description { get; set; }
        public string? LogoUrl { get; set; }
        public Guid UserId { get; set; }

        public CreateLeagueRequest(string name, string country, string? description = null, string? logoUrl = null, Guid userId = default)
        {
            Name = name;
            Country = country;
            Description = description;
            LogoUrl = logoUrl;
            UserId = userId;
        }
        
        // Parameterless constructor for deserialization
        public CreateLeagueRequest() { }
    }
}
