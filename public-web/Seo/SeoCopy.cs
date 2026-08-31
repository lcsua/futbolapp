namespace PublicWeb.Seo;

public sealed class SeoPageModel
{
    public string Title { get; init; } = "MiLiga";
    public string Description { get; init; } = "Resultados, posiciones, partidos y estadísticas de tu torneo.";
    public string CanonicalPath { get; init; } = "/";
    public string? OgImage { get; init; }
    public string? OgTitle { get; init; }
    public string? OgUrlPath { get; init; }
    public string OgType { get; init; } = "website";
    public bool NoIndex { get; init; }
    public bool LargeOgImage { get; init; }
    public string? H1 { get; init; }
    public IReadOnlyList<SeoBreadcrumbItem> Breadcrumbs { get; init; } = Array.Empty<SeoBreadcrumbItem>();
}

public sealed class SeoBreadcrumbItem
{
    public string Name { get; init; } = "";
    public string? Path { get; init; }
}

public static class SeoCopy
{
    public const string Brand = "MiLiga";

    public static string Title(string pageTitle) => $"{pageTitle} | {Brand}";

    public static SeoPageModel Home() => new()
    {
        Title = Title("Todo tu torneo, en un solo lugar"),
        Description = "Resultados, posiciones, partidos y estadísticas de tu liga amateur en MiLiga.",
        CanonicalPath = "/",
        Breadcrumbs = new[]
        {
            new SeoBreadcrumbItem { Name = "Inicio", Path = "/" }
        }
    };

    public static SeoPageModel LigasIndex() => new()
    {
        Title = Title("Ligas"),
        Description = "Explorá ligas amateur y torneos argentinos. Seguí fixture, resultados y posiciones en MiLiga.",
        CanonicalPath = "/ligas",
        H1 = "Ligas",
        Breadcrumbs = new[]
        {
            new SeoBreadcrumbItem { Name = "Inicio", Path = "/" },
            new SeoBreadcrumbItem { Name = "Ligas", Path = "/ligas" }
        }
    };

    public static SeoPageModel LeagueHome(string leagueName, string leagueSlug, string? seasonName, string? logoUrl, string? description)
    {
        var seasonBit = string.IsNullOrWhiteSpace(seasonName) ? "" : $" Seguí el {seasonName} en MiLiga.";
        var desc = !string.IsNullOrWhiteSpace(description)
            ? TrimDesc(description!)
            : $"Fixture, resultados, tabla de posiciones y equipos de la {leagueName}.{seasonBit}";

        return new SeoPageModel
        {
            Title = Title($"{leagueName} - Fixture, Resultados y Posiciones"),
            Description = desc,
            CanonicalPath = $"/ligas/{leagueSlug}",
            OgImage = logoUrl,
            H1 = leagueName,
            Breadcrumbs = LeagueCrumbs(leagueName, leagueSlug)
        };
    }

    public static SeoPageModel LeagueFixture(
        string leagueName,
        string leagueSlug,
        string? seasonName,
        string? logoUrl,
        string? divisionName = null)
    {
        var season = seasonName ?? "la temporada actual";
        var hasDivision = !string.IsNullOrWhiteSpace(divisionName);
        var title = hasDivision
            ? Title($"Fixture División {divisionName} {leagueName} - {season}")
            : Title($"Fixture {leagueName} - {season}");
        var desc = hasDivision
            ? $"Fixture de la División {divisionName} de la {leagueName}. Fechas, horarios y canchas del {season}."
            : $"Consultá el fixture completo de la {leagueName}, fechas, horarios, canchas y próximos partidos del {season}.";
        return new SeoPageModel
        {
            Title = title,
            Description = desc,
            CanonicalPath = $"/ligas/{leagueSlug}/fixture",
            OgTitle = hasDivision ? $"Fixture División {divisionName} · {leagueName}" : null,
            OgImage = hasDivision
                ? PublicShareLinks.ShareCardPath("fixture", divisionName!, leagueName, seasonName)
                : logoUrl,
            LargeOgImage = hasDivision,
            H1 = hasDivision ? $"Fixture División {divisionName}" : $"Fixture {leagueName}",
            Breadcrumbs = LeagueCrumbs(leagueName, leagueSlug, "Fixture", $"/ligas/{leagueSlug}/fixture")
        };
    }

    public static SeoPageModel LeagueStandings(
        string leagueName,
        string leagueSlug,
        string? seasonName,
        string? logoUrl,
        string? divisionName = null)
    {
        var season = seasonName ?? "la temporada actual";
        var hasDivision = !string.IsNullOrWhiteSpace(divisionName);
        var useCard = PublicShareLinks.UseCustomOgCard("posiciones", divisionName);
        var title = hasDivision
            ? Title($"Posiciones División {divisionName} {leagueName} - {season}")
            : Title($"Tabla de Posiciones {leagueName} - {season}");
        var desc = hasDivision
            ? $"Tabla de posiciones de la División {divisionName} de la {leagueName}. Posiciones actualizadas del {season}."
            : $"Tabla de posiciones actualizada de la {leagueName}. Posiciones por división del {season}.";
        return new SeoPageModel
        {
            Title = title,
            Description = desc,
            CanonicalPath = $"/ligas/{leagueSlug}/posiciones",
            OgTitle = useCard ? $"Posiciones División {divisionName} · {leagueName}" : null,
            OgImage = useCard
                ? PublicShareLinks.ShareCardPath("posiciones", divisionName!, leagueName, seasonName)
                : logoUrl,
            LargeOgImage = useCard,
            H1 = hasDivision ? $"Posiciones División {divisionName}" : $"Tabla de Posiciones {leagueName}",
            Breadcrumbs = LeagueCrumbs(leagueName, leagueSlug, "Posiciones", $"/ligas/{leagueSlug}/posiciones")
        };
    }

    public static SeoPageModel LeagueResults(
        string leagueName,
        string leagueSlug,
        string? seasonName,
        string? logoUrl,
        string? divisionName = null)
    {
        var season = seasonName ?? "la temporada actual";
        var hasDivision = !string.IsNullOrWhiteSpace(divisionName);
        var title = hasDivision
            ? Title($"Resultados División {divisionName} {leagueName} - {season}")
            : Title($"Resultados {leagueName} - {season}");
        var desc = hasDivision
            ? $"Últimos resultados de la División {divisionName} de la {leagueName}. Resultados por fecha del {season}."
            : $"Últimos resultados de la {leagueName}. Resultados por fecha y división del {season}.";
        return new SeoPageModel
        {
            Title = title,
            Description = desc,
            CanonicalPath = $"/ligas/{leagueSlug}/resultados",
            OgTitle = hasDivision ? $"Resultados División {divisionName} · {leagueName}" : null,
            OgImage = hasDivision
                ? PublicShareLinks.ShareCardPath("resultados", divisionName!, leagueName, seasonName)
                : logoUrl,
            LargeOgImage = hasDivision,
            H1 = hasDivision ? $"Resultados División {divisionName}" : $"Resultados {leagueName}",
            Breadcrumbs = LeagueCrumbs(leagueName, leagueSlug, "Resultados", $"/ligas/{leagueSlug}/resultados")
        };
    }

    public static SeoPageModel LeagueInformation(string leagueName, string leagueSlug, string? logoUrl, string? description)
    {
        var desc = !string.IsNullOrWhiteSpace(description)
            ? TrimDesc(description!)
            : $"Información, reglamentos y documentos de la {leagueName} en MiLiga.";

        return new SeoPageModel
        {
            Title = Title($"Información {leagueName}"),
            Description = desc,
            CanonicalPath = $"/ligas/{leagueSlug}/informacion",
            OgImage = logoUrl,
            H1 = $"Información {leagueName}",
            Breadcrumbs = LeagueCrumbs(leagueName, leagueSlug, "Información", $"/ligas/{leagueSlug}/informacion")
        };
    }

    public static SeoPageModel MatchPage(
        string homeName,
        string awayName,
        string? leagueName,
        string? leagueSlug,
        Guid matchId,
        string? logoUrl)
    {
        var vs = $"{homeName} vs {awayName}";
        var league = string.IsNullOrWhiteSpace(leagueName) ? "MiLiga" : leagueName;
        var crumbs = new List<SeoBreadcrumbItem>
        {
            new() { Name = "Inicio", Path = "/" },
            new() { Name = "Ligas", Path = "/ligas" }
        };
        if (!string.IsNullOrWhiteSpace(leagueSlug) && !string.IsNullOrWhiteSpace(leagueName))
            crumbs.Add(new SeoBreadcrumbItem { Name = leagueName, Path = $"/ligas/{leagueSlug}" });
        crumbs.Add(new SeoBreadcrumbItem { Name = vs, Path = $"/partido/{matchId}" });

        return new SeoPageModel
        {
            Title = Title($"{vs} - {league}"),
            Description = $"Resultado, horario y detalles de {vs} en la {league}.",
            CanonicalPath = $"/partido/{matchId}",
            OgImage = logoUrl,
            H1 = vs,
            Breadcrumbs = crumbs
        };
    }

    public static SeoPageModel TeamPage(string teamName, string leagueName, string leagueSlug, string teamSlug, string? logoUrl)
    {
        return new SeoPageModel
        {
            Title = Title($"{teamName} - {leagueName}"),
            Description = $"Fixture, resultados y estadísticas de {teamName} en la {leagueName}.",
            CanonicalPath = $"/ligas/{leagueSlug}/{teamSlug}",
            OgImage = logoUrl,
            H1 = teamName,
            Breadcrumbs = new[]
            {
                new SeoBreadcrumbItem { Name = "Inicio", Path = "/" },
                new SeoBreadcrumbItem { Name = "Ligas", Path = "/ligas" },
                new SeoBreadcrumbItem { Name = leagueName, Path = $"/ligas/{leagueSlug}" },
                new SeoBreadcrumbItem { Name = teamName, Path = $"/ligas/{leagueSlug}/{teamSlug}" }
            }
        };
    }

    public static SeoPageModel NotFound() => new()
    {
        Title = Title("Página no disponible"),
        Description = "MiLiga sigue creciendo. Esa sección todavía no está disponible.",
        CanonicalPath = "/",
        NoIndex = true,
        H1 = "Seguimos creciendo"
    };

    private static SeoBreadcrumbItem[] LeagueCrumbs(string leagueName, string leagueSlug, string? page = null, string? pagePath = null)
    {
        var list = new List<SeoBreadcrumbItem>
        {
            new() { Name = "Inicio", Path = "/" },
            new() { Name = "Ligas", Path = "/ligas" },
            new() { Name = leagueName, Path = $"/ligas/{leagueSlug}" }
        };
        if (!string.IsNullOrWhiteSpace(page))
            list.Add(new SeoBreadcrumbItem { Name = page!, Path = pagePath });
        return list.ToArray();
    }

    private static string TrimDesc(string value)
    {
        var t = value.Trim();
        if (t.Length <= 160) return t;
        return t[..157].TrimEnd() + "…";
    }
}
