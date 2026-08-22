using Microsoft.Extensions.Options;

namespace PublicWeb.Seo;

public sealed class SeoUrlBuilder
{
    private readonly SeoOptions _options;

    public SeoUrlBuilder(IOptions<SeoOptions> options)
    {
        _options = options.Value;
    }

    public string PublicBaseUrl => _options.PublicBaseUrl.TrimEnd('/');

    public bool AllowIndexing => _options.AllowIndexing;

    public string Absolute(string? pathAndQuery)
    {
        var path = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery.Trim();
        if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return path;

        if (!path.StartsWith('/'))
            path = "/" + path;

        return PublicBaseUrl + path;
    }

    public string AbsoluteMedia(string? urlOrPath)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
            return Absolute(_options.DefaultOgImagePath);
        return Absolute(urlOrPath);
    }

    public string DefaultOgImage => Absolute(_options.DefaultOgImagePath);

    public string LeagueHome(string leagueSlug) => Absolute($"/ligas/{leagueSlug}");
    public string LeagueFixture(string leagueSlug) => Absolute($"/ligas/{leagueSlug}/fixture");
    public string LeagueStandings(string leagueSlug) => Absolute($"/ligas/{leagueSlug}/posiciones");
    public string LeagueResults(string leagueSlug) => Absolute($"/ligas/{leagueSlug}/resultados");
    public string LeagueInformation(string leagueSlug) => Absolute($"/ligas/{leagueSlug}/informacion");
    public string Team(string leagueSlug, string teamSlug) => Absolute($"/ligas/{leagueSlug}/{teamSlug}");
    public string ArgentineCompetition(string slug) => Absolute($"/ligas/argentina/{slug}");
}
