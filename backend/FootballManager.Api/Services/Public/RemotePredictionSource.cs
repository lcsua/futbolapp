using System.Net.Http.Json;
using System.Text.Json;
using FootballManager.Api.Models.Public;
using FootballManager.Application.Predictions;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

/// <summary>
/// Builds predictions from the public production API (standings, results and open fixtures)
/// so a local site can show real championship data without a production database connection.
/// </summary>
public sealed class RemotePredictionSource
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PredictionCache _predictions;
    private readonly ILogger<RemotePredictionSource> _logger;

    public RemotePredictionSource(
        IHttpClientFactory httpClientFactory,
        PredictionCache predictions,
        ILogger<RemotePredictionSource> logger)
    {
        _httpClientFactory = httpClientFactory;
        _predictions = predictions;
        _logger = logger;
    }

    public async Task<PredictionsPublicDto?> GetAsync(
        string leagueSlug,
        string? seasonSlug,
        string? divisionSlug,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("PredictionSource");
        var meta = await ReadAsync<List<SeasonPublicDto>>(client, $"liga/{Uri.EscapeDataString(leagueSlug)}/meta", cancellationToken);
        if (meta == null || meta.Count == 0)
            return null;

        var season = ResolveSeason(meta, seasonSlug);
        if (season == null)
        {
            return new PredictionsPublicDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                Warning = "No hay un campeonato público para estimar."
            };
        }

        var table = await ReadAsync<SeasonGroupedDto<StandingsRowPublicDto>>(
            client,
            $"liga/{Uri.EscapeDataString(leagueSlug)}/tabla?season={Uri.EscapeDataString(season.Slug)}",
            cancellationToken);
        if (table == null)
        {
            return new PredictionsPublicDto
            {
                SeasonName = season.Name,
                SeasonSlug = season.Slug,
                GeneratedAtUtc = DateTime.UtcNow,
                Warning = "No se pudo leer la tabla oficial para calcular las predicciones."
            };
        }

        var teamDivision = new Dictionary<Guid, Guid>();
        var teams = new Dictionary<Guid, TeamPublicDto>();
        foreach (var group in table.Divisions)
        {
            var division = MatchDivision(season, group.DivisionSlug, group.DivisionName);
            if (division == null)
                continue;
            foreach (var row in group.Data)
            {
                if (row.Team.Id == Guid.Empty)
                    continue;
                teamDivision[row.Team.Id] = division.Id;
                Remember(teams, row.Team);
            }
        }

        var selected = season.Divisions.OrderBy(division => division.Name, StringComparer.OrdinalIgnoreCase).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(divisionSlug) && !divisionSlug.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var target = divisionSlug.Trim();
            selected = selected.Where(division =>
                division.Slug.Equals(target, StringComparison.OrdinalIgnoreCase)
                || division.Name.Equals(target, StringComparison.OrdinalIgnoreCase));
        }

        var page = new PredictionsPublicDto
        {
            SeasonName = string.IsNullOrWhiteSpace(table.SeasonName) ? season.Name : table.SeasonName,
            SeasonSlug = string.IsNullOrWhiteSpace(table.SeasonSlug) ? season.Slug : table.SeasonSlug,
            GeneratedAtUtc = DateTime.UtcNow
        };

        var playedOutsideTable = false;
        foreach (var division in selected)
        {
            var divisionSeasonId = Mix(season.Id, division.Id);
            var results = await ReadAsync<SeasonGroupedDto<MatchdayGroupDto>>(
                client,
                $"liga/{Uri.EscapeDataString(leagueSlug)}/resultados?season={Uri.EscapeDataString(season.Slug)}&division={Uri.EscapeDataString(division.Slug)}",
                cancellationToken);
            var upcoming = await ReadAsync<SeasonGroupedDto<MatchdayGroupDto>>(
                client,
                $"liga/{Uri.EscapeDataString(leagueSlug)}/partidos?season={Uri.EscapeDataString(season.Slug)}&division={Uri.EscapeDataString(division.Slug)}",
                cancellationToken);

            var matches = new Dictionary<Guid, MatchPublicDto>();
            Collect(results, division.Slug, matches);
            Collect(upcoming, division.Slug, matches);

            var sources = new List<PredictionSourceFixture>(matches.Count);
            foreach (var match in matches.Values)
            {
                Remember(teams, match.HomeTeam);
                Remember(teams, match.AwayTeam);
                var hasScore = match.HomeScore.HasValue && match.AwayScore.HasValue;
                if (string.Equals(match.Status, "PLAYED", StringComparison.OrdinalIgnoreCase) && hasScore)
                    playedOutsideTable = true;

                sources.Add(new PredictionSourceFixture(
                    match.Id,
                    match.RoundNumber,
                    match.Kickoff == default ? null : match.Kickoff,
                    match.Status,
                    hasScore,
                    match.HomeScore,
                    match.AwayScore,
                    divisionSeasonId,
                    match.HomeTeam.Id,
                    match.HomeTeam.Name,
                    SideDivision(season.Id, division.Id, divisionSeasonId, match.HomeTeam.Id, teamDivision),
                    match.AwayTeam.Id,
                    match.AwayTeam.Name,
                    SideDivision(season.Id, division.Id, divisionSeasonId, match.AwayTeam.Id, teamDivision)));
            }

            var rows = table.Divisions
                .FirstOrDefault(group => SameDivision(group.DivisionSlug, group.DivisionName, division))
                ?.Data
                .Select(row => new TeamStandingDto(
                    row.Position,
                    row.Team.Id,
                    string.IsNullOrWhiteSpace(row.Team.Name) ? "Equipo" : row.Team.Name,
                    row.Points,
                    row.Played,
                    row.Won,
                    row.Drawn,
                    row.Lost,
                    row.GoalsFor,
                    row.GoalsAgainst,
                    row.GoalsFor - row.GoalsAgainst))
                .ToList()
                ?? new List<TeamStandingDto>();

            var input = PredictionComposer.Build(division.Id, division.Name, divisionSeasonId, rows, sources);
            var fingerprint = PredictionFingerprint.Compute(input);
            var cacheKey = $"pred:remote:{leagueSlug}:{season.Slug}:{division.Slug}:{fingerprint}";
            var cached = await _predictions.GetOrCreateAsync(
                cacheKey,
                () =>
                {
                    var outlook = ChampionshipPredictor.Predict(input);
                    _logger.LogInformation(
                        "Predicciones remotas {League} {Division} sims={Sims} ms={Ms} pending={Pending} published={Published}",
                        leagueSlug,
                        division.Name,
                        outlook.Simulations,
                        outlook.ElapsedMilliseconds,
                        outlook.PendingMatches,
                        outlook.Published);
                    return new CachedPrediction
                    {
                        Outlook = outlook,
                        GeneratedAtUtc = DateTime.UtcNow
                    };
                },
                TimeSpan.FromHours(6),
                cancellationToken);

            page.Divisions.Add(PredictionComposer.ToDto(
                cached.Outlook,
                division.Name,
                division.Slug,
                cached.GeneratedAtUtc,
                teams));
            page.GeneratedAtUtc = cached.GeneratedAtUtc;
        }

        if (playedOutsideTable)
        {
            page.Warning = "Hay partidos marcados como jugados que la tabla oficial no suma. Las predicciones no los modifican ni los vuelven a calcular.";
        }

        return page;
    }

    private async Task<T?> ReadAsync<T>(HttpClient client, string path, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("La API pública respondió {Status} en {Path}", (int)response.StatusCode, path);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "No se pudo leer {Path} desde la API pública", path);
            return default;
        }
    }

    private static SeasonPublicDto? ResolveSeason(IReadOnlyList<SeasonPublicDto> seasons, string? seasonSlug)
    {
        if (!string.IsNullOrWhiteSpace(seasonSlug))
        {
            var match = seasons.FirstOrDefault(season =>
                season.Slug.Equals(seasonSlug, StringComparison.OrdinalIgnoreCase)
                || season.Name.Equals(seasonSlug, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                return match;
        }

        return seasons.FirstOrDefault(season => season.IsActive) ?? seasons.FirstOrDefault();
    }

    private static DivisionPublicDto? MatchDivision(SeasonPublicDto season, string slug, string name)
    {
        return season.Divisions.FirstOrDefault(division => SameDivision(slug, name, division));
    }

    private static bool SameDivision(string slug, string name, DivisionPublicDto division) =>
        (!string.IsNullOrWhiteSpace(slug) && division.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
        || (!string.IsNullOrWhiteSpace(name) && division.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static void Collect(SeasonGroupedDto<MatchdayGroupDto>? page, string divisionSlug, Dictionary<Guid, MatchPublicDto> matches)
    {
        if (page == null)
            return;

        var group = page.Divisions.FirstOrDefault(division =>
            division.DivisionSlug.Equals(divisionSlug, StringComparison.OrdinalIgnoreCase));
        if (group == null && page.Divisions.Count == 1)
            group = page.Divisions[0];
        if (group == null)
            return;

        foreach (var round in group.Data)
        {
            foreach (var match in round.Matches)
            {
                if (match.Id != Guid.Empty)
                    matches[match.Id] = match;
            }
        }
    }

    private static Guid SideDivision(
        Guid seasonId,
        Guid divisionId,
        Guid divisionSeasonId,
        Guid teamId,
        IReadOnlyDictionary<Guid, Guid> teamDivision)
    {
        if (teamId == Guid.Empty || !teamDivision.TryGetValue(teamId, out var known) || known == divisionId)
            return divisionSeasonId;
        return Mix(seasonId, known);
    }

    private static Guid Mix(Guid seasonId, Guid divisionId)
    {
        var left = seasonId.ToByteArray();
        var right = divisionId.ToByteArray();
        for (var i = 0; i < left.Length; i++)
            left[i] ^= right[i];
        return new Guid(left);
    }

    private static void Remember(Dictionary<Guid, TeamPublicDto> teams, TeamPublicDto? team)
    {
        if (team == null || team.Id == Guid.Empty || teams.ContainsKey(team.Id))
            return;

        teams[team.Id] = new TeamPublicDto
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            ShortName = team.ShortName,
            LogoUrl = SecureLogo(team.LogoUrl),
            LogoThumbUrl = SecureLogo(team.LogoThumbUrl)
        };
    }

    private static string? SecureLogo(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return "https://" + url["http://".Length..];
        return url;
    }
}
