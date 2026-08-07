namespace FootballManager.Application.ProfessionalFootball;

public sealed class ProfessionalCompetitionDefinition
{
    public required string Slug { get; init; }
    public required string Name { get; init; }
    public required string Country { get; init; }
    public required string Provider { get; init; }
    public required string ExternalCode { get; init; }
    public string? LogoUrl { get; init; }
}

public static class ProfessionalCompetitionsCatalog
{
    public static readonly ProfessionalCompetitionDefinition LigaProfesional = new()
    {
        Slug = "liga-profesional",
        Name = "Liga Profesional Argentina",
        Country = "Argentina",
        Provider = "espn",
        ExternalCode = "arg.1",
        LogoUrl = "https://a.espncdn.com/i/leaguelogos/soccer/500/745.png",
    };

    public static IReadOnlyList<ProfessionalCompetitionDefinition> All { get; } =
        new[] { LigaProfesional };

    public static ProfessionalCompetitionDefinition? GetBySlug(string slug) =>
        All.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));
}
