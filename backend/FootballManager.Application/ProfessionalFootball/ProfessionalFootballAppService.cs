using Microsoft.Extensions.Caching.Memory;

namespace FootballManager.Application.ProfessionalFootball;

public sealed class ProfessionalFootballAppService : IProfessionalFootballAppService
{
    private static readonly TimeZoneInfo ArgentinaTz = ResolveArgentinaTz();

    private readonly IProfessionalFootballProvider _provider;
    private readonly IMemoryCache _cache;

    public ProfessionalFootballAppService(IProfessionalFootballProvider provider, IMemoryCache cache)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<IReadOnlyList<CompetitionSummaryDto>> GetCompetitionsAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<CompetitionSummaryDto>();
        foreach (var def in ProfessionalCompetitionsCatalog.All)
        {
            var summary = await GetCompetitionSummaryAsync(def.Slug, cancellationToken);
            if (summary != null)
                list.Add(summary);
            else
            {
                list.Add(new CompetitionSummaryDto(
                    def.Slug, def.Name, def.Country, def.LogoUrl,
                    DateTime.UtcNow.Year, string.Empty, null));
            }
        }

        return list;
    }

    public async Task<CompetitionSummaryDto?> GetCompetitionSummaryAsync(string slug, CancellationToken cancellationToken = default)
    {
        var def = ProfessionalCompetitionsCatalog.GetBySlug(slug);
        if (def == null)
            return null;

        var cacheKey = $"profootball:summary:{def.Slug}";
        if (_cache.TryGetValue(cacheKey, out CompetitionSummaryDto? cached) && cached != null)
            return cached;

        try
        {
            var tournament = await ResolveCurrentAsync(def.ExternalCode, forStandings: false, cancellationToken);
            if (tournament == null)
                return null;

            var dto = new CompetitionSummaryDto(
                def.Slug,
                def.Name,
                def.Country,
                def.LogoUrl,
                tournament.SeasonYear,
                tournament.Name,
                tournament.SeasonTypeId);

            _cache.Set(cacheKey, dto, TimeSpan.FromHours(6));
            return dto;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CompetitionDetailDto?> GetCompetitionDetailAsync(string slug, CancellationToken cancellationToken = default)
    {
        var def = ProfessionalCompetitionsCatalog.GetBySlug(slug);
        if (def == null)
            return null;

        var cacheKey = $"profootball:detail:{def.Slug}";
        if (_cache.TryGetValue(cacheKey, out CompetitionDetailDto? cached) && cached != null)
            return cached;

        try
        {
            var displayTournament = await ResolveCurrentAsync(def.ExternalCode, forStandings: false, cancellationToken);
            if (displayTournament == null)
                return null;

            var standingsTournament = await ResolveCurrentAsync(def.ExternalCode, forStandings: true, cancellationToken)
                ?? displayTournament;

            var standings = await _provider.GetStandingsAsync(
                def.ExternalCode, standingsTournament.SeasonYear, standingsTournament.SeasonTypeId, cancellationToken);

            var todayAr = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ArgentinaTz).Date;
            var from = DateOnly.FromDateTime(todayAr);
            var to = from.AddDays(10);
            var matches = await _provider.GetMatchesAsync(
                def.ExternalCode, displayTournament.SeasonYear, displayTournament.SeasonTypeId, from, to, cancellationToken);

            var upcoming = matches
                .Where(m => !string.Equals(m.Status, "post", StringComparison.OrdinalIgnoreCase))
                .Where(m => m.Date >= DateTimeOffset.UtcNow.AddHours(-2) || IsPreOrLive(m.Status))
                .OrderBy(m => m.Date)
                .Take(20)
                .ToList();

            var summary = new CompetitionSummaryDto(
                def.Slug,
                def.Name,
                def.Country,
                def.LogoUrl,
                displayTournament.SeasonYear,
                displayTournament.Name,
                displayTournament.SeasonTypeId);

            var detail = new CompetitionDetailDto(summary, standings, upcoming);
            _cache.Set(cacheKey, detail, TimeSpan.FromMinutes(10));
            return detail;
        }
        catch
        {
            return null;
        }
    }

    private async Task<CurrentTournament?> ResolveCurrentAsync(
        string externalCode,
        bool forStandings,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"profootball:seasons:{externalCode}";
        if (!_cache.TryGetValue(cacheKey, out IReadOnlyList<SeasonInfo>? seasons) || seasons == null)
        {
            seasons = await _provider.GetSeasonsAsync(externalCode, cancellationToken);
            _cache.Set(cacheKey, seasons, TimeSpan.FromHours(12));
        }

        var now = DateTimeOffset.UtcNow;
        return forStandings
            ? CurrentTournamentResolver.ResolveForStandings(seasons, now)
            : CurrentTournamentResolver.Resolve(seasons, now);
    }

    private static bool IsPreOrLive(string status) =>
        status.Equals("pre", StringComparison.OrdinalIgnoreCase)
        || status.Equals("in", StringComparison.OrdinalIgnoreCase);

    private static TimeZoneInfo ResolveArgentinaTz()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }
    }
}
