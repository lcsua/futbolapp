using FootballManager.Api.Helpers;
using FootballManager.Api.Models.Public;
using FootballManager.Application.Standings;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

public sealed record SimulatorDivisionInput(
    Guid DivisionSeasonId,
    Guid DivisionId,
    string Name,
    IReadOnlyList<Guid> RosterTeamIds);

public sealed record SimulatorFixtureInput(
    Guid Id,
    int RoundNumber,
    DateTime? Kickoff,
    string Status,
    bool HasResult,
    Guid FixtureDivisionSeasonId,
    Guid HomeTeamId,
    Guid HomeDivisionSeasonId,
    Guid AwayTeamId,
    Guid AwayDivisionSeasonId);

/// <summary>
/// Builds the read-only simulator payload from the published table and the fixture.
/// It does not write scores. Pending matches keep their official scores off the form.
/// </summary>
public static class SimulatorComposer
{
    public static SimulatorPublicDto Compose(
        string seasonName,
        string seasonSlug,
        string? divisionSlug,
        IReadOnlyList<SimulatorDivisionInput> divisions,
        IReadOnlyList<DivisionStandingsDto> standings,
        IReadOnlyList<SimulatorFixtureInput> fixtures,
        IReadOnlyDictionary<Guid, TeamPublicDto> teams)
    {
        var rules = StandingsRules.Official;
        var limitation = rules.UnsupportedReason();
        var playedWithScore = fixtures.Count(fixture =>
            IsStatus(fixture.Status, "PLAYED") && fixture.HasResult);

        var model = new SimulatorPublicDto
        {
            SeasonName = seasonName,
            SeasonSlug = seasonSlug,
            Supported = limitation == null,
            Limitation = limitation,
            Warning = playedWithScore > 0
                ? "Hay partidos marcados como jugados que la tabla oficial no suma. El simulador no los modifica ni los vuelve a calcular."
                : null,
            Rules = new SimulatorRulesPublicDto
            {
                WinPoints = rules.WinPoints,
                DrawPoints = rules.DrawPoints,
                LossPoints = rules.LossPoints,
                TieBreakers = rules.TieBreakers.ToList(),
                Adjustments = new List<SimulatorAdjustmentPublicDto>()
            }
        };

        var selected = divisions
            .OrderBy(division => division.Name, StringComparer.OrdinalIgnoreCase)
            .AsEnumerable();
        if (!string.IsNullOrWhiteSpace(divisionSlug) && !divisionSlug.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var target = SlugHelper.NormalizeSlug(divisionSlug);
            selected = selected.Where(division => SlugHelper.NormalizeSlug(division.Name) == target);
        }

        foreach (var division in selected)
        {
            var official = standings.FirstOrDefault(item => item.DivisionId == division.DivisionId);
            var touching = fixtures.Where(fixture => Belongs(fixture, division.DivisionSeasonId)).ToList();
            var pending = touching.Where(IsPending).ToList();

            var rounds = pending
                .GroupBy(fixture => fixture.RoundNumber)
                .OrderBy(group => group.Key)
                .Select(group => new SimulatorRoundPublicDto
                {
                    Round = group.Key,
                    Matches = group
                        .OrderBy(fixture => fixture.Kickoff ?? DateTime.MaxValue)
                        .ThenBy(fixture => Team(teams, fixture.HomeTeamId).Name, StringComparer.OrdinalIgnoreCase)
                        .Select(fixture => ToMatch(fixture, division.DivisionSeasonId, teams))
                        .ToList(),
                    Byes = Byes(division, touching, group.Key, teams)
                })
                .ToList();

            model.Divisions.Add(new SimulatorDivisionPublicDto
            {
                DivisionName = division.Name,
                DivisionSlug = SlugHelper.NormalizeSlug(division.Name),
                PendingCount = pending.Count,
                Official = official?.Standings.Select(row => ToOfficial(row, teams)).ToList()
                    ?? new List<SimulatorOfficialRowPublicDto>(),
                Rounds = rounds
            });
        }

        return model;
    }

    private static bool Belongs(SimulatorFixtureInput fixture, Guid divisionSeasonId) =>
        fixture.FixtureDivisionSeasonId == divisionSeasonId
        || fixture.HomeDivisionSeasonId == divisionSeasonId
        || fixture.AwayDivisionSeasonId == divisionSeasonId;

    private static bool IsPending(SimulatorFixtureInput fixture)
    {
        if (fixture.HomeTeamId == Guid.Empty || fixture.AwayTeamId == Guid.Empty)
            return false;
        if (IsStatus(fixture.Status, "CANCELLED"))
            return false;
        if (IsStatus(fixture.Status, "COMPLETED") && fixture.HasResult)
            return false;
        if (IsStatus(fixture.Status, "PLAYED") && fixture.HasResult)
            return false;
        return true;
    }

    private static List<SimulatorByePublicDto> Byes(
        SimulatorDivisionInput division,
        IReadOnlyList<SimulatorFixtureInput> touching,
        int round,
        IReadOnlyDictionary<Guid, TeamPublicDto> teams)
    {
        var playing = touching
            .Where(fixture => fixture.RoundNumber == round)
            .SelectMany(fixture => new[] { fixture.HomeTeamId, fixture.AwayTeamId })
            .Where(id => id != Guid.Empty)
            .ToHashSet();

        return division.RosterTeamIds
            .Where(id => id != Guid.Empty && !playing.Contains(id))
            .Select(id => Team(teams, id))
            .OrderBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select(team => new SimulatorByePublicDto { Team = team })
            .ToList();
    }

    private static SimulatorMatchPublicDto ToMatch(
        SimulatorFixtureInput fixture,
        Guid divisionSeasonId,
        IReadOnlyDictionary<Guid, TeamPublicDto> teams) =>
        new()
        {
            Id = fixture.Id,
            Round = fixture.RoundNumber,
            Kickoff = fixture.Kickoff,
            Status = fixture.Status,
            StatusLabel = StatusLabel(fixture.Status),
            Home = Team(teams, fixture.HomeTeamId),
            Away = Team(teams, fixture.AwayTeamId),
            HomeCounts = fixture.HomeDivisionSeasonId == divisionSeasonId,
            AwayCounts = fixture.AwayDivisionSeasonId == divisionSeasonId
        };

    private static SimulatorOfficialRowPublicDto ToOfficial(
        TeamStandingDto row,
        IReadOnlyDictionary<Guid, TeamPublicDto> teams)
    {
        var team = Team(teams, row.TeamId);
        if (string.IsNullOrWhiteSpace(team.Name))
            team.Name = row.TeamName;
        if (string.IsNullOrWhiteSpace(team.Slug))
            team.Slug = SlugHelper.NormalizeSlug(row.TeamName);

        return new SimulatorOfficialRowPublicDto
        {
            Position = row.Position,
            Team = team,
            Played = row.Played,
            Won = row.Wins,
            Drawn = row.Draws,
            Lost = row.Losses,
            GoalsFor = row.GoalsFor,
            GoalsAgainst = row.GoalsAgainst,
            Points = row.Points
        };
    }

    private static TeamPublicDto Team(IReadOnlyDictionary<Guid, TeamPublicDto> teams, Guid teamId)
    {
        if (!teams.TryGetValue(teamId, out var team))
            return new TeamPublicDto { Id = teamId, Name = "Equipo", Slug = "equipo" };

        return new TeamPublicDto
        {
            Id = team.Id,
            Name = team.Name,
            Slug = team.Slug,
            ShortName = team.ShortName,
            LogoUrl = PublicLogo(team.LogoUrl),
            LogoThumbUrl = PublicLogo(team.LogoThumbUrl)
        };
    }

    /// <summary>Inline data URIs are real files but repeating them on every pending match makes the page unusable.</summary>
    private static string? PublicLogo(string? url) =>
        string.IsNullOrWhiteSpace(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? null
            : url;

    private static string StatusLabel(string status)
    {
        if (IsStatus(status, "COMPLETED") || IsStatus(status, "PLAYED"))
            return "Finalizado";
        if (IsStatus(status, "IN_PROGRESS"))
            return "En juego";
        if (IsStatus(status, "SUSPENDED"))
            return "Suspendido";
        if (IsStatus(status, "CANCELLED"))
            return "Cancelado";
        if (IsStatus(status, "POSTPONED"))
            return "Aplazado";
        return "Programado";
    }

    private static bool IsStatus(string status, string expected) =>
        string.Equals(status, expected, StringComparison.OrdinalIgnoreCase);
}
