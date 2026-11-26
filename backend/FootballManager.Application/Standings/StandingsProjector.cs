using System;
using System.Collections.Generic;
using System.Linq;

namespace FootballManager.Application.Standings;

/// <summary>
/// Official table rules. Every public league uses this set today:
/// 3 points for a win, 1 for a draw, tie-break by points, goal difference, then goals for.
/// Point adjustments cover sanctions. None are stored yet, so the published table has none.
/// A future Monte Carlo run should call <see cref="StandingsProjector.FromBaseline"/>
/// once per sample instead of inventing another ranking.
/// </summary>
public sealed record StandingsRules(int WinPoints, int DrawPoints, int LossPoints, IReadOnlyList<string> TieBreakers)
{
    public static StandingsRules Official { get; } = new(
        3,
        1,
        0,
        new[] { "points", "goalDifference", "goalsFor" });

    public string? UnsupportedReason()
    {
        if (WinPoints != 3 || DrawPoints != 1 || LossPoints != 0)
            return "Este campeonato no usa la puntuación de 3 puntos por victoria y 1 por empate. El simulador no arma una tabla para no mostrar un orden incorrecto.";

        if (TieBreakers.Count != 3
            || !string.Equals(TieBreakers[0], "points", StringComparison.Ordinal)
            || !string.Equals(TieBreakers[1], "goalDifference", StringComparison.Ordinal)
            || !string.Equals(TieBreakers[2], "goalsFor", StringComparison.Ordinal))
        {
            return "Este campeonato usa un desempate que el simulador todavía no aplica. No mostramos una tabla proyectada para no inventar posiciones.";
        }

        return null;
    }
}

public sealed record StandingSide(Guid TeamId, string TeamName, Guid DivisionId, string DivisionName);

public sealed record CountedResult(StandingSide Home, StandingSide Away, int HomeGoals, int AwayGoals);

/// <summary>Points added on top of match results. Negative values are sanctions.</summary>
public sealed record PointAdjustment(Guid TeamId, string TeamName, Guid DivisionId, string DivisionName, int Points);

public sealed record BaselineTeam(
    Guid TeamId,
    string TeamName,
    int Position,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int Points);

/// <summary>
/// One hypothetical score. Null or incomplete goals are ignored.
/// Negative goals and totals above 99 are ignored so a partial input never counts as a draw.
/// </summary>
public sealed record SimulatedScore(
    Guid HomeTeamId,
    string HomeName,
    bool HomeCounts,
    Guid AwayTeamId,
    string AwayName,
    bool AwayCounts,
    int? HomeGoals,
    int? AwayGoals);

public sealed record ProjectedTeam(
    int Position,
    Guid TeamId,
    string TeamName,
    int Played,
    int Wins,
    int Draws,
    int Losses,
    int GoalsFor,
    int GoalsAgainst,
    int Points,
    int GoalDifference,
    int OfficialPosition,
    int PointsDelta);

public sealed record ProjectedDivision(Guid DivisionId, string DivisionName, IReadOnlyList<ProjectedTeam> Teams);

public sealed record Projection(
    bool Supported,
    string? Limitation,
    IReadOnlyList<ProjectedDivision> Divisions,
    int SimulatedMatches);

public static class StandingsProjector
{
    public static Projection FromResults(
        IEnumerable<CountedResult> results,
        IEnumerable<PointAdjustment>? adjustments = null,
        StandingsRules? rules = null)
    {
        rules ??= StandingsRules.Official;
        var limitation = rules.UnsupportedReason();
        if (limitation != null)
            return new Projection(false, limitation, Array.Empty<ProjectedDivision>(), 0);

        var map = new Dictionary<(Guid DivisionId, Guid TeamId), Acc>();
        var seen = 0;
        foreach (var result in results)
        {
            AddMatchSide(map, result.Home, result.HomeGoals, result.AwayGoals, rules, ref seen);
            AddMatchSide(map, result.Away, result.AwayGoals, result.HomeGoals, rules, ref seen);
        }

        if (adjustments != null)
        {
            foreach (var adjustment in adjustments)
            {
                var acc = GetOrAdd(map, adjustment.DivisionId, adjustment.DivisionName, adjustment.TeamId, adjustment.TeamName, ref seen);
                acc.Points += adjustment.Points;
            }
        }

        var divisions = map.Values
            .GroupBy(acc => (acc.DivisionId, acc.DivisionName))
            .OrderBy(group => group.Key.DivisionName, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ProjectedDivision(
                group.Key.DivisionId,
                group.Key.DivisionName,
                Rank(group, trackDelta: false)))
            .ToList();

        return new Projection(true, null, divisions, 0);
    }

    /// <summary>
    /// Projects one division from the published rows plus hypothetical scores.
    /// With no complete scores the order matches <paramref name="baseline"/>.
    /// Call this once per Monte Carlo sample.
    /// </summary>
    public static Projection FromBaseline(
        Guid divisionId,
        string divisionName,
        IEnumerable<BaselineTeam> baseline,
        IEnumerable<SimulatedScore> scores,
        IEnumerable<PointAdjustment>? extraAdjustments = null,
        StandingsRules? rules = null)
    {
        rules ??= StandingsRules.Official;
        var limitation = rules.UnsupportedReason();
        if (limitation != null)
            return new Projection(false, limitation, Array.Empty<ProjectedDivision>(), 0);

        var map = new Dictionary<(Guid DivisionId, Guid TeamId), Acc>();
        var seen = 0;
        foreach (var team in baseline)
        {
            var acc = GetOrAdd(map, divisionId, divisionName, team.TeamId, team.TeamName, ref seen);
            acc.Seen = team.Position > 0 ? team.Position : int.MaxValue;
            acc.OfficialPosition = team.Position;
            acc.Played = team.Played;
            acc.Wins = team.Wins;
            acc.Draws = team.Draws;
            acc.Losses = team.Losses;
            acc.GoalsFor = team.GoalsFor;
            acc.GoalsAgainst = team.GoalsAgainst;
            acc.Points = team.Points;
            acc.BaselinePoints = team.Points;
        }

        if (extraAdjustments != null)
        {
            foreach (var adjustment in extraAdjustments)
            {
                if (adjustment.DivisionId != Guid.Empty && adjustment.DivisionId != divisionId)
                    continue;
                var acc = GetOrAdd(map, divisionId, divisionName, adjustment.TeamId, adjustment.TeamName, ref seen);
                if (acc.OfficialPosition == 0)
                    acc.Seen = int.MaxValue;
                acc.Points += adjustment.Points;
            }
        }

        var simulated = 0;
        foreach (var score in scores)
        {
            if (!score.HomeCounts && !score.AwayCounts)
                continue;
            if (!TryReadScore(score.HomeGoals, score.AwayGoals, out var homeGoals, out var awayGoals))
                continue;

            simulated++;
            if (score.HomeCounts)
            {
                var acc = GetOrAdd(map, divisionId, divisionName, score.HomeTeamId, score.HomeName, ref seen);
                if (acc.OfficialPosition == 0)
                    acc.Seen = int.MaxValue;
                Apply(acc, homeGoals, awayGoals, rules);
            }

            if (score.AwayCounts)
            {
                var acc = GetOrAdd(map, divisionId, divisionName, score.AwayTeamId, score.AwayName, ref seen);
                if (acc.OfficialPosition == 0)
                    acc.Seen = int.MaxValue;
                Apply(acc, awayGoals, homeGoals, rules);
            }
        }

        var division = new ProjectedDivision(divisionId, divisionName, Rank(map.Values, trackDelta: true));
        return new Projection(true, null, new[] { division }, simulated);
    }

    public static bool TryReadScore(int? homeGoals, int? awayGoals, out int home, out int away)
    {
        home = 0;
        away = 0;
        if (homeGoals is not int homeValue || awayGoals is not int awayValue)
            return false;
        if (homeValue < 0 || awayValue < 0 || homeValue > 99 || awayValue > 99)
            return false;
        home = homeValue;
        away = awayValue;
        return true;
    }

    private static void AddMatchSide(
        Dictionary<(Guid DivisionId, Guid TeamId), Acc> map,
        StandingSide side,
        int goalsFor,
        int goalsAgainst,
        StandingsRules rules,
        ref int seen)
    {
        var acc = GetOrAdd(map, side.DivisionId, side.DivisionName, side.TeamId, side.TeamName, ref seen);
        Apply(acc, goalsFor, goalsAgainst, rules);
    }

    private static void Apply(Acc acc, int goalsFor, int goalsAgainst, StandingsRules rules)
    {
        acc.Played++;
        acc.GoalsFor += goalsFor;
        acc.GoalsAgainst += goalsAgainst;
        if (goalsFor > goalsAgainst)
        {
            acc.Wins++;
            acc.Points += rules.WinPoints;
        }
        else if (goalsFor < goalsAgainst)
        {
            acc.Losses++;
            acc.Points += rules.LossPoints;
        }
        else
        {
            acc.Draws++;
            acc.Points += rules.DrawPoints;
        }
    }

    private static Acc GetOrAdd(
        Dictionary<(Guid DivisionId, Guid TeamId), Acc> map,
        Guid divisionId,
        string divisionName,
        Guid teamId,
        string teamName,
        ref int seen)
    {
        var key = (divisionId, teamId);
        if (map.TryGetValue(key, out var existing))
        {
            if (string.IsNullOrWhiteSpace(existing.Name) && !string.IsNullOrWhiteSpace(teamName))
                existing.Name = teamName;
            return existing;
        }

        seen++;
        var created = new Acc
        {
            DivisionId = divisionId,
            DivisionName = divisionName,
            TeamId = teamId,
            Name = teamName ?? string.Empty,
            Seen = seen
        };
        map[key] = created;
        return created;
    }

    private static IReadOnlyList<ProjectedTeam> Rank(IEnumerable<Acc> teams, bool trackDelta)
    {
        return teams
            .OrderByDescending(team => team.Points)
            .ThenByDescending(team => team.GoalsFor - team.GoalsAgainst)
            .ThenByDescending(team => team.GoalsFor)
            .ThenBy(team => team.Seen)
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => new ProjectedTeam(
                index + 1,
                team.TeamId,
                team.Name,
                team.Played,
                team.Wins,
                team.Draws,
                team.Losses,
                team.GoalsFor,
                team.GoalsAgainst,
                team.Points,
                team.GoalsFor - team.GoalsAgainst,
                trackDelta ? team.OfficialPosition : index + 1,
                trackDelta ? team.Points - team.BaselinePoints : 0))
            .ToList();
    }

    private sealed class Acc
    {
        public Guid DivisionId { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public Guid TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Seen { get; set; }
        public int OfficialPosition { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public int BaselinePoints { get; set; }
    }
}
