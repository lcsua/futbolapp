using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using PublicWeb.Seo;

namespace PublicWeb.Tests;

public class SeoCopyTests
{
    [Fact]
    public void LeagueHome_Title_IsUniqueAndBranded()
    {
        var page = SeoCopy.LeagueHome("Liga de Veteranos de Perico", "veteranos-de-perico", "Clausura 2026", null, null);
        Assert.Contains("Liga de Veteranos de Perico", page.Title);
        Assert.Contains("MiLiga", page.Title);
        Assert.Equal("/ligas/veteranos-de-perico", page.CanonicalPath);
        Assert.DoesNotContain("?", page.CanonicalPath);
    }

    [Fact]
    public void Fixture_Canonical_StripsFilters()
    {
        var page = SeoCopy.LeagueFixture("Liga de Veteranos de Perico", "veteranos-de-perico", "Clausura 2026", null);
        Assert.Equal("/ligas/veteranos-de-perico/fixture", page.CanonicalPath);
        Assert.StartsWith("Fixture Liga de Veteranos de Perico", page.H1);
        Assert.Contains("Clausura 2026", page.Title);
        Assert.False(page.LargeOgImage);
    }

    [Fact]
    public void Standings_AnyDivision_UsesShareCard()
    {
        var page = SeoCopy.LeagueStandings("Liga de Veteranos de Perico", "veteranos-de-perico", "Clausura 2026", "/logo.png", "B");
        Assert.Contains("División B", page.Title);
        Assert.Contains("/og/share.jpg", page.OgImage);
        Assert.True(page.LargeOgImage);
        Assert.Equal("/ligas/veteranos-de-perico/posiciones", page.CanonicalPath);
    }

    [Fact]
    public void Results_WithDivision_UsesShareCard()
    {
        var page = SeoCopy.LeagueResults("Liga de Veteranos de Perico", "veteranos-de-perico", "Clausura 2026", "/logo.png", "B");
        Assert.StartsWith("Resultados División B", page.OgTitle);
        Assert.Contains("kind=resultados", page.OgImage);
    }

    [Fact]
    public void ShareLinks_SectionPath_IncludesSeasonAndDivision()
    {
        var path = PublicShareLinks.SectionPath("veteranos-de-perico", "resultados", "clausura-2026", "b");
        Assert.Equal("/ligas/veteranos-de-perico/resultados?season=clausura-2026&division=b", path);
    }

    [Fact]
    public void TeamPage_HasSpecificMetadata()
    {
        var page = SeoCopy.TeamPage("B° Malvinas Las Pts", "Liga de Veteranos de Perico", "veteranos-de-perico", "bmalvinas-las-pts", null);
        Assert.Equal("/ligas/veteranos-de-perico/bmalvinas-las-pts", page.CanonicalPath);
        Assert.Equal("B° Malvinas Las Pts", page.H1);
        Assert.Contains("B° Malvinas Las Pts", page.Description);
    }

    [Fact]
    public void MatchPage_HasSpecificMetadata()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var page = SeoCopy.MatchPage("ATL FOR EVER", "AC. LA UNION", "Liga de Veteranos", "veteranos", id, null);
        Assert.Equal("/partido/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", page.CanonicalPath);
        Assert.Equal("ATL FOR EVER vs AC. LA UNION", page.H1);
        Assert.Contains("ATL FOR EVER", page.Title);
        Assert.Contains("Liga de Veteranos", page.Title);
    }

    [Fact]
    public void NotFound_IsNoIndex()
    {
        var page = SeoCopy.NotFound();
        Assert.True(page.NoIndex);
    }
}

public class OgShareImageGeneratorTests
{
    [Fact]
    public void Render_ProducesJpeg()
    {
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var gen = new OgShareImageGenerator(new StubEnv { ContentRootPath = webRoot }, cache);
        var jpeg = gen.Render("resultados", "B", "Liga de Veteranos de Perico", "Clausura 2026");
        Assert.True(jpeg.Length > 2000);
        Assert.Equal(0xFF, jpeg[0]);
        Assert.Equal(0xD8, jpeg[1]);
        Assert.Equal(0xFF, jpeg[2]);
    }

    private sealed class StubEnv : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "PublicWeb";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; } = "Tests";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}

public class SitemapXmlBuilderTests
{
    [Fact]
    public void BuildUrlSet_ContainsLocAndOptionalLastmod()
    {
        var xml = SitemapXmlBuilder.BuildUrlSet(new[]
        {
            new SitemapUrlEntry { Loc = "https://miliga.com.ar/" },
            new SitemapUrlEntry
            {
                Loc = "https://miliga.com.ar/ligas/veteranos-de-perico",
                LastMod = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc)
            }
        });

        Assert.Contains("<urlset", xml);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico", xml);
        Assert.Contains("<lastmod>2026-08-12</lastmod>", xml);
        Assert.DoesNotContain("/admin", xml);
    }

    [Fact]
    public void BuildUrlSet_EscapesXml()
    {
        var xml = SitemapXmlBuilder.BuildUrlSet(new[]
        {
            new SitemapUrlEntry { Loc = "https://miliga.com.ar/a&b" }
        });
        Assert.Contains("a&amp;b", xml);
    }
}

public class SeoUrlBuilderTests
{
    [Fact]
    public void Absolute_UsesConfiguredPublicBase()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new SeoOptions
        {
            PublicBaseUrl = "https://miliga.com.ar"
        });
        var urls = new SeoUrlBuilder(opts);
        Assert.Equal("https://miliga.com.ar/ligas/veteranos-de-perico/fixture", urls.LeagueFixture("veteranos-de-perico"));
        Assert.Equal("https://miliga.com.ar/branding/blue/icon-512.png", urls.DefaultOgImage);
    }
}

public class RobotsTxtTests
{
    [Fact]
    public void Robots_AllowsPublic_AndPointsToSitemap()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new SeoOptions
        {
            PublicBaseUrl = "https://miliga.com.ar"
        });
        var urls = new SeoUrlBuilder(opts);
        var svc = new SitemapDocumentService(
            new FakeHttpClientFactory(),
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            urls,
            new PublicWeb.Services.Public.ProfessionalFootballPublicService(
                new FakeHttpClientFactory(),
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PublicWeb.Services.Public.ProfessionalFootballPublicService>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SitemapDocumentService>.Instance);

        var robots = svc.GetRobotsTxt();
        Assert.Contains("User-agent: *", robots);
        Assert.Contains("Allow: /", robots);
        Assert.Contains("Disallow: /admin", robots);
        Assert.Contains("Sitemap: https://miliga.com.ar/sitemap.xml", robots);
        Assert.DoesNotContain("Disallow: /ligas", robots);
    }

    [Fact]
    public void Robots_WhenIndexingDisabled_DisallowsAll()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new SeoOptions
        {
            PublicBaseUrl = "https://develop.miliga.com.ar",
            AllowIndexing = false
        });
        var urls = new SeoUrlBuilder(opts);
        var svc = new SitemapDocumentService(
            new FakeHttpClientFactory(),
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            urls,
            new PublicWeb.Services.Public.ProfessionalFootballPublicService(
                new FakeHttpClientFactory(),
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PublicWeb.Services.Public.ProfessionalFootballPublicService>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SitemapDocumentService>.Instance);

        var robots = svc.GetRobotsTxt();
        Assert.Contains("User-agent: *", robots);
        Assert.Contains("Disallow: /", robots);
        Assert.DoesNotContain("Sitemap:", robots);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new FakeHandler()) { BaseAddress = new Uri("http://127.0.0.1/api/public/") };
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{"generatedAtUtc":"2026-08-12T00:00:00Z","leagues":[]}""", System.Text.Encoding.UTF8, "application/json")
            });
    }
}

public class SitemapVeteranosExampleTests
{
    [Fact]
    public async Task BuildEntries_IncludesLeagueSectionAndTeamUrls()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new SeoOptions
        {
            PublicBaseUrl = "https://miliga.com.ar"
        });
        var urls = new SeoUrlBuilder(opts);
        var handler = new SequencedHandler();
        var factory = new HandlerFactory(handler);
        var svc = new SitemapDocumentService(
            factory,
            new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
            urls,
            new PublicWeb.Services.Public.ProfessionalFootballPublicService(
                factory,
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PublicWeb.Services.Public.ProfessionalFootballPublicService>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SitemapDocumentService>.Instance);

        var entries = await svc.BuildEntriesAsync();
        var locs = entries.Select(e => e.Loc).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("https://miliga.com.ar/", locs);
        Assert.Contains("https://miliga.com.ar/ligas", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico/fixture", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico/posiciones", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico/resultados", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico/informacion", locs);
        Assert.Contains("https://miliga.com.ar/ligas/veteranos-de-perico/bmalvinas-las-pts", locs);
        Assert.DoesNotContain(locs, l => l.Contains("/admin", StringComparison.OrdinalIgnoreCase));

        var xml = await svc.GetSitemapXmlAsync();
        Assert.Contains("<urlset", xml);
        Assert.Contains("veteranos-de-perico/bmalvinas-las-pts", xml);
    }

    private sealed class HandlerFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public HandlerFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false) { BaseAddress = new Uri("http://127.0.0.1/api/public/") };
    }

    private sealed class SequencedHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            string body;
            if (path.EndsWith("/sitemap", StringComparison.OrdinalIgnoreCase))
            {
                body = """
                {
                  "generatedAtUtc": "2026-08-12T12:00:00Z",
                  "leagues": [
                    {
                      "slug": "veteranos-de-perico",
                      "updatedAtUtc": "2026-08-10T00:00:00Z",
                      "teams": [
                        { "slug": "bmalvinas-las-pts", "updatedAtUtc": "2026-08-11T00:00:00Z" }
                      ]
                    }
                  ]
                }
                """;
            }
            else
            {
                body = "[]";
            }

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
