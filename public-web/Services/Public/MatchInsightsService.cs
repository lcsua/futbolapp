using System.Globalization;
using PublicWeb.Models.Public;

namespace PublicWeb.Services.Public;

/// <summary>
/// Deterministic, template-based match notes. No external services.
/// The same input always yields the same insights, ordered by priority.
/// </summary>
public sealed class MatchInsightsService
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    private readonly MatchInsightThresholds _t;

    public MatchInsightsService(MatchInsightThresholds? thresholds = null)
    {
        _t = thresholds ?? MatchInsightThresholds.Default;
    }

    public IReadOnlyList<MatchInsight> Select(MatchInsightInput input)
    {
        var ranked = Evaluate(input)
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.Type, StringComparer.Ordinal)
            .ThenBy(i => i.TeamId?.ToString("N") ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        var picked = new List<MatchInsight>();
        foreach (var insight in ranked)
        {
            if (picked.Count >= _t.MaxInsights) break;
            if (Conflicts(picked, insight)) continue;
            picked.Add(insight);
        }

        Balance(picked, ranked, input);
        return picked
            .OrderByDescending(i => i.Priority)
            .ThenBy(i => i.Type, StringComparer.Ordinal)
            .ThenBy(i => i.TeamId?.ToString("N") ?? string.Empty, StringComparer.Ordinal)
            .ToList();
    }

    private List<MatchInsight> Evaluate(MatchInsightInput input)
    {
        var insights = new List<MatchInsight>();
        var home = input.Home ?? new MatchSideInput();
        var away = input.Away ?? new MatchSideInput();
        var homeName = DisplayName(home, "El local");
        var awayName = DisplayName(away, "El visitante");

        AddStreaks(insights, home, homeName);
        AddStreaks(insights, away, awayName);
        AddGoodForm(insights, home, homeName);
        AddGoodForm(insights, away, awayName);
        AddFormContrast(insights, home, away, homeName, awayName);
        AddAttackAndDefenseRanks(insights, input, home, away);
        AddOffensiveGap(insights, home, away, homeName, awayName);
        AddDefensiveParity(insights, home, away, homeName, awayName);
        AddNotableAverage(insights, home, homeName);
        AddNotableAverage(insights, away, awayName);
        AddWinRate(insights, home, homeName);
        AddWinRate(insights, away, awayName);
        AddTableNotes(insights, home, away, homeName, awayName);
        return insights;
    }

    private void AddStreaks(List<MatchInsight> insights, MatchSideInput side, string name)
    {
        var recent = side.Recent ?? Array.Empty<RecentResultInput>();
        if (recent.Count == 0) return;

        var wins = Streak(recent, r => r.GoalsFor > r.GoalsAgainst);
        var unbeaten = Streak(recent, r => r.GoalsFor >= r.GoalsAgainst);
        var losses = Streak(recent, r => r.GoalsFor < r.GoalsAgainst);
        var winless = Streak(recent, r => r.GoalsFor <= r.GoalsAgainst);
        var clean = Streak(recent, r => r.GoalsAgainst == 0);

        if (wins >= _t.MinConsecutiveWins && wins >= unbeaten)
        {
            var (gf, ga) = Sum(recent, wins);
            insights.Add(Insight(
                "win-streak",
                100 + wins,
                $"{CountWord(wins)} victorias consecutivas",
                $"{name} llega al encuentro tras ganar sus últimos {CountLower(wins)} partidos, {ScoringClause(gf, ga)}.",
                "flame",
                side.TeamId));
        }
        else if (unbeaten >= _t.MinUnbeaten)
        {
            var slice = recent.Take(unbeaten).ToList();
            var w = slice.Count(r => r.GoalsFor > r.GoalsAgainst);
            var d = unbeaten - w;
            insights.Add(Insight(
                "unbeaten",
                88 + unbeaten,
                $"{CountWord(unbeaten)} partidos sin perder",
                $"{name} acumula {CountLower(unbeaten)} partidos consecutivos sin perder, con {CountPhrase(w, "victoria", "victorias")} y {CountPhrase(d, "empate", "empates")}.",
                "trend",
                side.TeamId));
        }

        if (clean >= _t.MinCleanSheets && clean > wins)
        {
            insights.Add(Insight(
                "clean-sheets",
                90 + clean,
                $"{CountWord(clean)} partidos sin recibir goles",
                $"{name} lleva {CountLower(clean)} partidos consecutivos sin recibir goles.",
                "shield",
                side.TeamId));
        }

        if (losses >= _t.MinLosses)
        {
            insights.Add(Insight(
                "loss-streak",
                96 + losses,
                $"{CountWord(losses)} derrotas consecutivas",
                $"{name} llega al encuentro tras perder sus últimos {CountLower(losses)} partidos.",
                "down",
                side.TeamId));
        }
        else if (winless >= _t.MinWinless)
        {
            insights.Add(Insight(
                "winless",
                84 + winless,
                $"{CountWord(winless)} partidos sin ganar",
                $"{name} acumula {CountLower(winless)} partidos consecutivos sin ganar.",
                "down",
                side.TeamId));
        }
    }

    private void AddGoodForm(List<MatchInsight> insights, MatchSideInput side, string name)
    {
        var window = Window(side);
        if (window.Count < _t.MinRecentSample) return;
        if (Streak(window, r => r.GoalsFor > r.GoalsAgainst) >= window.Count) return;

        var points = window.Sum(PointsOf);
        var possible = window.Count * 3;
        if (possible <= 0 || points / (double)possible < _t.GoodFormRate) return;

        var lastWasLoss = window[0].GoalsFor < window[0].GoalsAgainst;
        var extra = lastWasLoss ? ", a pesar de haber perdido su último encuentro" : string.Empty;
        insights.Add(Insight(
            "good-form",
            76,
            "Llega en buena forma",
            $"{name} consiguió {points} de los últimos {possible} puntos posibles{extra}.",
            "flame",
            side.TeamId));
    }

    private void AddFormContrast(
        List<MatchInsight> insights,
        MatchSideInput home,
        MatchSideInput away,
        string homeName,
        string awayName)
    {
        var homeWindow = Window(home);
        var awayWindow = Window(away);
        if (homeWindow.Count < _t.MinRecentSample || awayWindow.Count < _t.MinRecentSample) return;

        var homePts = homeWindow.Sum(PointsOf);
        var awayPts = awayWindow.Sum(PointsOf);
        var homeRate = homePts / (double)(homeWindow.Count * 3);
        var awayRate = awayPts / (double)(awayWindow.Count * 3);

        string strongName;
        string weakName;
        int strongPts;
        int weakPts;
        int strongPossible;
        int weakPossible;
        int strongCount;
        int weakCount;
        if (homeRate >= _t.GoodFormRate && awayRate <= _t.PoorFormRate && homePts - awayPts >= _t.MinFormContrastGap)
        {
            strongName = homeName;
            weakName = awayName;
            strongPts = homePts;
            weakPts = awayPts;
            strongPossible = homeWindow.Count * 3;
            weakPossible = awayWindow.Count * 3;
            strongCount = homeWindow.Count;
            weakCount = awayWindow.Count;
        }
        else if (awayRate >= _t.GoodFormRate && homeRate <= _t.PoorFormRate && awayPts - homePts >= _t.MinFormContrastGap)
        {
            strongName = awayName;
            weakName = homeName;
            strongPts = awayPts;
            weakPts = homePts;
            strongPossible = awayWindow.Count * 3;
            weakPossible = homeWindow.Count * 3;
            strongCount = awayWindow.Count;
            weakCount = homeWindow.Count;
        }
        else
        {
            return;
        }

        insights.Add(Insight(
            "form-contrast",
            80,
            "Contraste de rendimiento",
            $"{strongName} sumó {strongPts} de {strongPossible} puntos en sus últimos {CountLower(strongCount)} partidos. {weakName} sumó {weakPts} de {weakPossible}.",
            "contrast",
            null));
    }

    private void AddAttackAndDefenseRanks(
        List<MatchInsight> insights,
        MatchInsightInput input,
        MatchSideInput home,
        MatchSideInput away)
    {
        var band = Comparable(input.Division, _t.MinPlayedForRates, _t.MaxPlayedGapForRanks);
        if (band.Count < 2) return;

        var bestGoals = band.Max(r => r.GoalsFor);
        var attackers = band.Where(r => r.GoalsFor == bestGoals).ToList();
        if (attackers.Count == 1)
        {
            var side = SideMatching(attackers[0].TeamId, home, away);
            if (side != null)
            {
                insights.Add(Insight(
                    "best-attack",
                    72,
                    "El mejor ataque de la división",
                    $"{DisplayName(side, "El equipo")} convirtió {CountPhrase(attackers[0].GoalsFor, "gol", "goles")} en {attackers[0].Played} partidos, la marca más alta {DeDivision(input.DivisionName)}.",
                    "swords",
                    side.TeamId));
            }
        }

        var bestDefense = band.Min(r => r.GoalsAgainst);
        var defenders = band.Where(r => r.GoalsAgainst == bestDefense).ToList();
        if (defenders.Count == 1)
        {
            var side = SideMatching(defenders[0].TeamId, home, away);
            if (side != null)
            {
                insights.Add(Insight(
                    "best-defense",
                    70,
                    "La mejor defensa de la división",
                    $"{DisplayName(side, "El equipo")} recibió {CountPhrase(defenders[0].GoalsAgainst, "gol", "goles")} en {defenders[0].Played} partidos, la marca más baja {DeDivision(input.DivisionName)}.",
                    "shield",
                    side.TeamId));
            }
        }
    }

    private void AddOffensiveGap(
        List<MatchInsight> insights,
        MatchSideInput home,
        MatchSideInput away,
        string homeName,
        string awayName)
    {
        if (!home.HasStanding || !away.HasStanding) return;
        if (home.Played < _t.MinPlayedForRates || away.Played < _t.MinPlayedForRates) return;

        var homeAvg = home.GoalsFor / (double)home.Played;
        var awayAvg = away.GoalsFor / (double)away.Played;
        var high = Math.Max(homeAvg, awayAvg);
        var low = Math.Min(homeAvg, awayAvg);
        if (high - low < _t.MinOffensiveGap) return;
        if (low <= 0)
        {
            if (high < _t.MinOffensiveRatio) return;
        }
        else if (high / low < _t.MinOffensiveRatio)
        {
            return;
        }

        var better = homeAvg >= awayAvg ? homeName : awayName;
        var worse = homeAvg >= awayAvg ? awayName : homeName;
        var betterAvg = homeAvg >= awayAvg ? homeAvg : awayAvg;
        var worseAvg = homeAvg >= awayAvg ? awayAvg : homeAvg;
        insights.Add(Insight(
            "offensive-gap",
            68,
            "Diferencia ofensiva",
            $"{better} promedia {Avg(betterAvg)} goles por partido, mientras que {worse} promedia {Avg(worseAvg)}.",
            "swords",
            null));
    }

    private void AddDefensiveParity(
        List<MatchInsight> insights,
        MatchSideInput home,
        MatchSideInput away,
        string homeName,
        string awayName)
    {
        if (!home.HasStanding || !away.HasStanding) return;
        if (home.Played < _t.MinPlayedForRates || away.Played < _t.MinPlayedForRates) return;
        if (Math.Abs(home.Played - away.Played) > _t.MaxPlayedGapForRanks) return;

        var homeAvg = home.GoalsAgainst / (double)home.Played;
        var awayAvg = away.GoalsAgainst / (double)away.Played;
        var sameTotals = home.GoalsAgainst == away.GoalsAgainst;
        var close = Math.Abs(home.GoalsAgainst - away.GoalsAgainst) <= 1 && Math.Abs(homeAvg - awayAvg) <= 0.2;
        if (!sameTotals && !close) return;

        string description;
        if (sameTotals && home.Played == away.Played)
        {
            description = home.GoalsAgainst == 0
                ? $"{homeName} y {awayName} todavía no recibieron goles en {home.Played} partidos."
                : $"Ambos equipos recibieron {home.GoalsAgainst} goles en {home.Played} encuentros, con un promedio de {Avg(homeAvg)} goles en contra por partido.";
        }
        else if (sameTotals)
        {
            description = $"{homeName} y {awayName} recibieron {home.GoalsAgainst} goles: {homeName} en {home.Played} partidos y {awayName} en {away.Played}.";
        }
        else
        {
            description = $"{homeName} recibió {CountPhrase(home.GoalsAgainst, "gol", "goles")} en {home.Played} partidos y {awayName} recibió {CountPhrase(away.GoalsAgainst, "gol", "goles")} en {away.Played}. El promedio es casi el mismo ({Avg(homeAvg)} y {Avg(awayAvg)}).";
        }

        insights.Add(Insight("defensive-parity", 66, "Igualdad defensiva", description, "shield", null));
    }

    private void AddNotableAverage(List<MatchInsight> insights, MatchSideInput side, string name)
    {
        if (!side.HasStanding || side.Played < _t.MinPlayedForRates) return;
        var avg = side.GoalsFor / (double)side.Played;
        if (avg < _t.NotableGoalsPerGame) return;

        insights.Add(Insight(
            "high-average",
            62,
            "Promedio ofensivo alto",
            $"{name} promedia {Avg(avg)} goles por partido, por encima de los tres por encuentro.",
            "flame",
            side.TeamId));
    }

    private void AddWinRate(List<MatchInsight> insights, MatchSideInput side, string name)
    {
        if (!side.HasStanding || side.Played < _t.MinPlayedForRates || side.Played <= 0) return;
        if (side.Wins / (double)side.Played < _t.HighWinRate) return;

        var recent = side.Recent ?? Array.Empty<RecentResultInput>();
        var wonAllLoaded = recent.Count > 0
            && recent.Count >= side.Played
            && recent.All(r => r.GoalsFor > r.GoalsAgainst);
        if (wonAllLoaded) return;

        var pct = (int)Math.Round(100.0 * side.Wins / side.Played, MidpointRounding.AwayFromZero);
        insights.Add(Insight(
            "win-rate",
            58,
            "Alto porcentaje de victorias",
            $"{name} ganó {CountPhrase(side.Wins, "partido", "partidos")} de los {side.Played} que disputó ({pct}%).",
            "flame",
            side.TeamId));
    }

    private void AddTableNotes(
        List<MatchInsight> insights,
        MatchSideInput home,
        MatchSideInput away,
        string homeName,
        string awayName)
    {
        if (home.HasStanding && away.HasStanding && home.Played >= 1 && away.Played >= 1 && home.Position == 1 && away.Position == 1)
        {
            insights.Add(Insight(
                "leader",
                56,
                "Llegan los líderes",
                $"{homeName} y {awayName} llegan como líderes de sus zonas.",
                "trophy",
                null));
        }
        else
        {
            var leader = home.HasStanding && home.Played >= 1 && home.Position == 1 ? home
                : away.HasStanding && away.Played >= 1 && away.Position == 1 ? away
                : null;
            if (leader != null)
            {
                insights.Add(Insight(
                    "leader",
                    56,
                    "Juega el líder",
                    $"{DisplayName(leader, "El líder")} llega como líder del campeonato, con {leader.Points} puntos, y buscará sostener la primera posición.",
                    "trophy",
                    leader.TeamId));
            }
        }

        if (home.HasStanding && away.HasStanding && home.Played >= 1 && away.Played >= 1)
        {
            var diff = Math.Abs(home.Points - away.Points);
            if (diff == 0)
            {
                insights.Add(Insight(
                    "points-gap",
                    57,
                    "Igualados en la tabla",
                    $"{homeName} y {awayName} llegan igualados con {home.Points} puntos.",
                    "scale",
                    null));
            }
            else if (diff <= _t.ClosePointsGap)
            {
                var verb = diff == 1 ? "separa" : "separan";
                var unit = diff == 1 ? "punto" : "puntos";
                insights.Add(Insight(
                    "points-gap",
                    54 + (_t.ClosePointsGap - diff),
                    "Al alcance en la tabla",
                    $"Solo {diff} {unit} {verb} a {homeName} y {awayName} en la tabla ({home.Points} y {away.Points}).",
                    "scale",
                    null));
            }

            var top = _t.TopClashPositions;
            if (home.Position > 1 && away.Position > 1
                && home.Position <= top && away.Position <= top)
            {
                insights.Add(Insight(
                    "top-five",
                    50,
                    "Choque de la parte alta",
                    $"{homeName} ({home.Position}.º) y {awayName} ({away.Position}.º) se enfrentan entre los primeros {top} de la tabla.",
                    "trophy",
                    null));
            }
        }
    }

    private void Balance(List<MatchInsight> picked, List<MatchInsight> ranked, MatchInsightInput input)
    {
        if (picked.Count < 2) return;
        var homeId = input.Home?.TeamId ?? Guid.Empty;
        var awayId = input.Away?.TeamId ?? Guid.Empty;
        if (homeId == Guid.Empty || awayId == Guid.Empty || homeId == awayId) return;

        bool Covers(MatchInsight insight, Guid id) => insight.TeamId == null || insight.TeamId == id;
        var coversHome = picked.Any(i => Covers(i, homeId));
        var coversAway = picked.Any(i => Covers(i, awayId));
        if (coversHome && coversAway) return;

        var missing = coversHome ? awayId : homeId;
        var candidate = ranked.FirstOrDefault(i =>
            i.TeamId == missing && picked.All(p => p.Type != i.Type || p.TeamId != i.TeamId));
        if (candidate == null || Conflicts(picked, candidate)) return;

        if (picked.Count < _t.MaxInsights)
        {
            picked.Add(candidate);
            return;
        }

        var victims = picked.Where(i => i.TeamId != null && i.TeamId != missing).ToList();
        if (victims.Count < 2) return;
        var victim = victims
            .OrderBy(i => i.Priority)
            .ThenByDescending(i => i.Type, StringComparer.Ordinal)
            .First();
        var remaining = picked.Where(p => !ReferenceEquals(p, victim)).ToList();
        if (Conflicts(remaining, candidate)) return;
        picked.Remove(victim);
        picked.Add(candidate);
    }

    private List<RecentResultInput> Window(MatchSideInput side)
    {
        if (!side.RecentKnown) return new List<RecentResultInput>();
        return (side.Recent ?? Array.Empty<RecentResultInput>()).Take(Math.Max(0, _t.RecentWindow)).ToList();
    }

    private static bool Conflicts(IReadOnlyList<MatchInsight> picked, MatchInsight next)
    {
        if (picked.Any(p => p.Type == next.Type && p.TeamId == next.TeamId)) return true;
        if (next.Type == "top-five" && picked.Any(p => p.Type == "leader")) return true;
        if (next.Type == "high-average" && picked.Any(p => p.Type == "best-attack" && p.TeamId == next.TeamId)) return true;
        if (next.Type == "good-form" && picked.Any(p =>
                p.Type == "form-contrast" || (p.Type == "win-streak" && p.TeamId == next.TeamId)))
            return true;
        if (next.Type == "clean-sheets" && picked.Any(p => p.Type == "win-streak" && p.TeamId == next.TeamId))
            return true;
        return false;
    }

    private static IReadOnlyList<DivisionStandingInput> Comparable(
        IReadOnlyList<DivisionStandingInput>? rows,
        int minPlayed,
        int playedGap)
    {
        var eligible = (rows ?? Array.Empty<DivisionStandingInput>())
            .Where(r => r.Played >= minPlayed)
            .ToList();
        if (eligible.Count < 2) return Array.Empty<DivisionStandingInput>();
        var maxPlayed = eligible.Max(r => r.Played);
        var band = eligible.Where(r => r.Played >= maxPlayed - Math.Max(0, playedGap)).ToList();
        return band.Count < 2 ? Array.Empty<DivisionStandingInput>() : band;
    }

    private static MatchSideInput? SideMatching(Guid teamId, MatchSideInput home, MatchSideInput away)
    {
        if (teamId == Guid.Empty) return null;
        if (home.TeamId == teamId) return home;
        if (away.TeamId == teamId) return away;
        return null;
    }

    private static int Streak(IReadOnlyList<RecentResultInput> recent, Func<RecentResultInput, bool> pred)
    {
        var count = 0;
        foreach (var result in recent)
        {
            if (!pred(result)) break;
            count++;
        }
        return count;
    }

    private static (int GoalsFor, int GoalsAgainst) Sum(IReadOnlyList<RecentResultInput> recent, int count)
    {
        var slice = recent.Take(count);
        return (slice.Sum(r => r.GoalsFor), slice.Sum(r => r.GoalsAgainst));
    }

    private static int PointsOf(RecentResultInput result)
    {
        if (result.GoalsFor > result.GoalsAgainst) return 3;
        if (result.GoalsFor == result.GoalsAgainst) return 1;
        return 0;
    }

    private static MatchInsight Insight(string type, int priority, string title, string description, string icon, Guid? teamId) =>
        new()
        {
            Type = type,
            Priority = priority,
            Title = title,
            Description = description,
            Icon = icon,
            TeamId = teamId == Guid.Empty ? null : teamId
        };

    private static string DisplayName(MatchSideInput side, string fallback) =>
        string.IsNullOrWhiteSpace(side.Name) ? fallback : side.Name.Trim();

    private static string DeDivision(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "de la división";
        var trimmed = name.Trim();
        return trimmed.Contains(' ') ? $"de {trimmed}" : $"de la división {trimmed}";
    }

    private static string Avg(double value) => value.ToString("0.00", EsAr);

    private static string ScoringClause(int goalsFor, int goalsAgainst)
    {
        var scored = CountPhrase(goalsFor, "gol", "goles");
        if (goalsAgainst <= 0) return $"convirtiendo {scored} y sin recibir goles";
        if (goalsAgainst == 1) return $"convirtiendo {scored} y recibiendo solamente uno";
        return $"convirtiendo {scored} y recibiendo {goalsAgainst}";
    }

    private static string CountPhrase(int value, string singular, string plural) =>
        value == 1 ? $"1 {singular}" : $"{value} {plural}";

    private static string CountWord(int value) => value switch
    {
        3 => "Tres",
        4 => "Cuatro",
        5 => "Cinco",
        6 => "Seis",
        7 => "Siete",
        8 => "Ocho",
        9 => "Nueve",
        10 => "Diez",
        _ => value.ToString(EsAr)
    };

    private static string CountLower(int value) => value switch
    {
        3 => "tres",
        4 => "cuatro",
        5 => "cinco",
        6 => "seis",
        7 => "siete",
        8 => "ocho",
        9 => "nueve",
        10 => "diez",
        _ => value.ToString(EsAr)
    };
}
