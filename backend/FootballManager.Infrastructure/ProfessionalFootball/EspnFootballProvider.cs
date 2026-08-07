using System.Globalization;
using System.Text.Json;
using FootballManager.Application.ProfessionalFootball;
using Microsoft.Extensions.Logging;

namespace FootballManager.Infrastructure.ProfessionalFootball;

public sealed class EspnFootballProvider : IProfessionalFootballProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<EspnFootballProvider> _logger;

    public EspnFootballProvider(HttpClient http, ILogger<EspnFootballProvider> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<SeasonInfo>> GetSeasonsAsync(string externalCode, CancellationToken cancellationToken = default)
    {
        var url = $"apis/common/v3/sports/soccer/{externalCode}/seasons";
        using var doc = await GetDocumentAsync(url, cancellationToken);
        return EspnSeasonsParser.Parse(doc.RootElement);
    }

    public async Task<IReadOnlyList<StandingGroupDto>> GetStandingsAsync(
        string externalCode,
        int season,
        string seasonType,
        CancellationToken cancellationToken = default)
    {
        var url =
            $"apis/v2/sports/soccer/{externalCode}/standings?season={season}&seasontype={Uri.EscapeDataString(seasonType)}&lang=es&region=ar";
        using var doc = await GetDocumentAsync(url, cancellationToken);
        return EspnStandingsParser.Parse(doc.RootElement);
    }

    public async Task<IReadOnlyList<ProfessionalMatchDto>> GetMatchesAsync(
        string externalCode,
        int season,
        string seasonType,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var from = fromDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var to = toDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var url =
            $"apis/site/v2/sports/soccer/{externalCode}/scoreboard?dates={from}-{to}&seasontype={Uri.EscapeDataString(seasonType)}&lang=es&region=ar";
        using var doc = await GetDocumentAsync(url, cancellationToken);
        return EspnScoreboardParser.Parse(doc.RootElement);
    }

    private async Task<JsonDocument> GetDocumentAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        _logger.LogDebug("ESPN GET {Url}", relativeUrl);
        using var response = await _http.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
