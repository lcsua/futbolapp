using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PublicWeb.Services.Public;

namespace PublicWeb.Seo;

public sealed class SitemapDocumentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly SeoUrlBuilder _urls;
    private readonly ProfessionalFootballPublicService _proService;
    private readonly ILogger<SitemapDocumentService> _logger;

    // Keep a single urlset until volume grows; index split is ready via BuildIndexXml.
    private const int SoftUrlBudget = 40_000;

    public SitemapDocumentService(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        SeoUrlBuilder urls,
        ProfessionalFootballPublicService proService,
        ILogger<SitemapDocumentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _urls = urls;
        _proService = proService;
        _logger = logger;
    }

    public async Task<string> GetSitemapXmlAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "seo_sitemap_xml_v1";
        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var entries = await BuildEntriesAsync(cancellationToken);
        var xml = entries.Count > SoftUrlBudget
            ? BuildIndexFallback(entries)
            : SitemapXmlBuilder.BuildUrlSet(entries);

        _cache.Set(cacheKey, xml, TimeSpan.FromMinutes(10));
        return xml;
    }

    public string GetRobotsTxt()
    {
        var sitemap = _urls.Absolute("/sitemap.xml");
        return
            $"""
            User-agent: *
            Allow: /

            # Private / operational surfaces
            Disallow: /admin
            Disallow: /admin/
            Disallow: /api/
            Disallow: /error/
            Disallow: /login

            Sitemap: {sitemap}

            """.Replace("\r\n", "\n");
    }

    public async Task<IReadOnlyList<SitemapUrlEntry>> BuildEntriesAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<SitemapUrlEntry>
        {
            new() { Loc = _urls.Absolute("/") },
            new() { Loc = _urls.Absolute("/ligas") }
        };

        var payload = await FetchBackendSitemapAsync(cancellationToken);
        if (payload?.Leagues != null)
        {
            foreach (var league in payload.Leagues)
            {
                if (string.IsNullOrWhiteSpace(league.Slug)) continue;
                var leagueMod = league.UpdatedAtUtc;
                list.Add(new SitemapUrlEntry { Loc = _urls.LeagueHome(league.Slug), LastMod = leagueMod });
                list.Add(new SitemapUrlEntry { Loc = _urls.LeagueFixture(league.Slug), LastMod = leagueMod });
                list.Add(new SitemapUrlEntry { Loc = _urls.LeagueStandings(league.Slug), LastMod = leagueMod });
                list.Add(new SitemapUrlEntry { Loc = _urls.LeagueResults(league.Slug), LastMod = leagueMod });
                list.Add(new SitemapUrlEntry { Loc = _urls.LeagueInformation(league.Slug), LastMod = leagueMod });

                foreach (var team in league.Teams ?? new())
                {
                    if (string.IsNullOrWhiteSpace(team.Slug)) continue;
                    list.Add(new SitemapUrlEntry
                    {
                        Loc = _urls.Team(league.Slug, team.Slug),
                        LastMod = team.UpdatedAtUtc ?? leagueMod
                    });
                }
            }
        }

        try
        {
            var (pros, failed) = await _proService.GetArgentineCompetitionsAsync();
            if (!failed)
            {
                foreach (var c in pros)
                {
                    if (string.IsNullOrWhiteSpace(c.Slug)) continue;
                    list.Add(new SitemapUrlEntry { Loc = _urls.ArgentineCompetition(c.Slug) });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not append Argentine competitions to sitemap");
        }

        return list
            .GroupBy(e => e.Loc, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.LastMod).First())
            .OrderBy(e => e.Loc, StringComparer.Ordinal)
            .ToList();
    }

    private string BuildIndexFallback(IReadOnlyList<SitemapUrlEntry> entries)
    {
        // Prepared for future split; currently still emits a single child sitemap URL.
        _logger.LogWarning("Sitemap url count {Count} exceeds soft budget; emitting sitemap index shell", entries.Count);
        return SitemapXmlBuilder.BuildSitemapIndex(new (string Loc, DateTime? LastMod)[]
        {
            (_urls.Absolute("/sitemaps/all.xml"), DateTime.UtcNow)
        });
    }

    private async Task<BackendSitemapDto?> FetchBackendSitemapAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("BackendApi");
            var uri = new Uri(client.BaseAddress!, "sitemap");
            return await client.GetFromJsonAsync<BackendSitemapDto>(uri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load sitemap payload from backend");
            return null;
        }
    }

    private sealed class BackendSitemapDto
    {
        public DateTime GeneratedAtUtc { get; set; }
        public List<BackendSitemapLeagueDto> Leagues { get; set; } = new();
    }

    private sealed class BackendSitemapLeagueDto
    {
        public string Slug { get; set; } = "";
        public DateTime? UpdatedAtUtc { get; set; }
        public List<BackendSitemapTeamDto> Teams { get; set; } = new();
    }

    private sealed class BackendSitemapTeamDto
    {
        public string Slug { get; set; } = "";
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
