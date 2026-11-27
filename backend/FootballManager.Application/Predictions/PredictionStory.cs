namespace FootballManager.Application.Predictions;

public sealed record TitleRaceReport(
    string Kind,
    string Headline,
    string Summary,
    int PointGap,
    int MatchesLeft,
    int PointsInPlay,
    IReadOnlyList<Guid> CandidateIds);

public sealed record TitleCampaign(string Marks, int Points, bool SecuresTitle);

public sealed record TitlePathReport(
    Guid TeamId,
    string Stance,
    string StanceLabel,
    string Summary,
    int CurrentPoints,
    int MaxPoints,
    int RivalMax,
    int? PointsToClinch,
    string PointsNote,
    int BestPosition,
    int WorstPosition,
    string PositionNote,
    bool CanClinchNextRound,
    string ClinchNote,
    IReadOnlyList<TitleCampaign> Campaigns);

public sealed record WhatIfRival(Guid TeamId, string Name, double IfWin, double IfDraw, double IfLoss);

public sealed record WhatIfReport(
    Guid TeamId,
    Guid MatchId,
    int Round,
    string OpponentName,
    double WinTitle,
    double DrawTitle,
    double LossTitle,
    bool WinReliable,
    bool DrawReliable,
    bool LossReliable,
    int WinSamples,
    int DrawSamples,
    int LossSamples,
    IReadOnlyList<WhatIfRival> Rivals);

/// <summary>Precomputed joint title chances for two teams' next-match outcomes (0=win,1=draw,2=loss).</summary>
public sealed record JointWhatIfBoard(
    Guid DefaultTeamAId,
    Guid DefaultTeamBId,
    IReadOnlyList<JointWhatIfTeamOption> Options,
    IReadOnlyList<JointWhatIfPair> Pairs);

public sealed record JointWhatIfTeamOption(
    Guid TeamId,
    string Name,
    Guid MatchId,
    int Round,
    string OpponentName,
    bool IsHome,
    int CurrentPoints,
    double TitleProbability);

public sealed record JointWhatIfPair(
    Guid TeamAId,
    Guid TeamBId,
    bool HeadToHead,
    IReadOnlyList<JointWhatIfCell> Cells);

public sealed record JointWhatIfCell(
    int OutcomeA,
    int OutcomeB,
    double TitleA,
    double TitleB,
    bool Reliable,
    int Samples,
    IReadOnlyList<JointWhatIfOther> Others);

public sealed record JointWhatIfOther(Guid TeamId, string Name, double TitleProbability);

public sealed record HistoryTeamPoint(
    Guid TeamId,
    double TitleProbability,
    double PodiumProbability,
    double ExpectedPosition,
    double ExpectedPoints);

public sealed record HistoryRound(
    int Round,
    bool Reconstructed,
    IReadOnlyList<HistoryTeamPoint> Teams);

public sealed record ProbabilityHistory(
    bool Available,
    bool Reconstructed,
    string Note,
    IReadOnlyList<HistoryRound> Rounds);

public static class TitleRaceCalculator
{
    public static TitleRaceReport Build(IReadOnlyList<TeamOutlook> teams, IReadOnlyList<PredictionFixture> pending)
    {
        var alive = teams
            .Where(team => !team.MathematicallyEliminated)
            .OrderByDescending(team => team.TitleProbability)
            .ThenBy(team => team.ExpectedPosition)
            .ToList();
        if (alive.Count == 0)
            alive = teams.OrderByDescending(team => team.TitleProbability).Take(1).ToList();

        var leader = alive[0];
        var second = alive.Count > 1 ? alive[1] : null;
        var gap = second == null ? leader.TitleProbability : leader.TitleProbability - second.TitleProbability;
        var close = alive.Where(team => team.TitleProbability >= 0.10).Take(4).ToList();
        var leaderPct = PredictionChance.Format(leader.TitleProbability);
        var secondPct = second == null ? null : PredictionChance.Format(second.TitleProbability);
        string kind;
        string headline;
        string summary;
        if (leader.MathematicallyCrowned || leader.TitleKind == "certain")
        {
            kind = "dominant";
            headline = $"{leader.Name} ya aseguró el título en la cuenta matemática ({leaderPct}).";
            summary = "Los puntos que tiene no los puede alcanzar el resto.";
        }
        else if (leader.TitleProbability >= 0.60 && gap >= 0.25)
        {
            kind = "dominant";
            headline = second == null
                ? $"{leader.Name} concentra la pelea con {leaderPct} de chances de título."
                : $"{leader.Name} lidera con {leaderPct}; {second.Name} lo persigue con {secondPct}.";
            summary = second == null
                ? "El resto del campeonato queda lejos en la simulación."
                : $"Ventaja de {PredictionChance.FormatPoints(gap * 100).Replace("+", string.Empty)} sobre el principal perseguidor.";
        }
        else if (close.Count is >= 2 and <= 3)
        {
            kind = "few";
            headline = string.Join(" · ", close.Select(team => $"{team.Name} {PredictionChance.Format(team.TitleProbability)}"));
            summary = close.Count == 2
                ? "Dos equipos concentran la pelea por el título."
                : "Un grupo chico concentra la lucha por el título.";
        }
        else
        {
            kind = "open";
            var top = alive.Take(Math.Min(3, alive.Count)).ToList();
            headline = top.Count <= 1
                ? "El campeonato sigue abierto."
                : $"Campeonato abierto: {string.Join(", ", top.Select(team => $"{team.Name} {PredictionChance.Format(team.TitleProbability)}"))}.";
            summary = "Ningún equipo concentra la simulación. Varios todavía pueden terminar primeros.";
        }

        var leaderMatches = Remaining(pending, leader.TeamId);
        var leaderPoints = leader.CurrentPoints;
        var secondPoints = second?.CurrentPoints ?? leaderPoints;
        return new TitleRaceReport(
            kind,
            headline,
            summary,
            Math.Abs(leaderPoints - secondPoints),
            leaderMatches,
            leaderMatches * 3,
            (close.Count > 0 ? close : alive.Take(2)).Select(team => team.TeamId).ToList());
    }

    public static int Remaining(IReadOnlyList<PredictionFixture> pending, Guid teamId) =>
        pending.Count(match => Counts(match, teamId));

    private static bool Counts(PredictionFixture match, Guid teamId) =>
        (match.Home.TeamId == teamId && match.Home.Counts)
        || (match.Away.TeamId == teamId && match.Away.Counts);
}

public static class TitlePathCalculator
{
    public static IReadOnlyList<TitlePathReport> Build(
        IReadOnlyList<TeamOutlook> teams,
        IReadOnlyList<PredictionFixture> pending)
    {
        var nextRound = pending.Count == 0 ? (int?)null : ChampionshipPredictor.NextFixtureRound(pending);
        var nextMatches = nextRound == null
            ? new List<PredictionFixture>()
            : pending.Where(match => match.Round == nextRound.Value).Take(10).ToList();
        var exhaustive = nextMatches.Count == pending.Count(match => match.Round == nextRound) && nextMatches.Count <= 10;

        return teams.Select(team => Describe(team, teams, pending, nextMatches, exhaustive)).ToList();
    }

    private static TitlePathReport Describe(
        TeamOutlook team,
        IReadOnlyList<TeamOutlook> teams,
        IReadOnlyList<PredictionFixture> pending,
        IReadOnlyList<PredictionFixture> nextMatches,
        bool exhaustive)
    {
        var mine = pending.Where(match => TitleRaceCalculator.Remaining(new[] { match }, team.TeamId) == 1).ToList();
        var maxPoints = team.CurrentPoints + mine.Count * 3;
        var rivalMax = 0;
        var rivalMaxIfIWinOut = 0;
        foreach (var rival in teams)
        {
            if (rival.TeamId == team.TeamId)
                continue;
            var rivalMatches = pending.Where(match => TitleRaceCalculator.Remaining(new[] { match }, rival.TeamId) == 1).ToList();
            var againstMe = rivalMatches.Count(match => Involves(match, team.TeamId) && Involves(match, rival.TeamId));
            rivalMax = Math.Max(rivalMax, rival.CurrentPoints + rivalMatches.Count * 3);
            rivalMaxIfIWinOut = Math.Max(rivalMaxIfIWinOut, rival.CurrentPoints + Math.Max(0, rivalMatches.Count - againstMe) * 3);
        }

        var winOutSecures = mine.Count > 0 && maxPoints > rivalMaxIfIWinOut;
        var pointsToClinch = rivalMax + 1;
        int? clinch = pointsToClinch <= maxPoints ? pointsToClinch : null;
        var pointsNote = clinch == null
            ? "No alcanza un número de puntos que asegure el título si cada rival gana todo lo que le queda, incluidos los cruces directos."
            : "Ese número supera el máximo de cada rival aunque ellos ganen todos sus partidos. Un empate en puntos no se cuenta como título asegurado.";

        var best = 1 + teams.Count(rival => rival.TeamId != team.TeamId && rival.CurrentPoints > maxPoints);
        var worst = WorstPosition(team, teams, pending, out var worstExact);
        var canPass = teams.Any(rival =>
            rival.TeamId != team.TeamId
            && rival.CurrentPoints <= maxPoints
            && rival.CurrentPoints + TitleRaceCalculator.Remaining(pending, rival.TeamId) * 3 > maxPoints);
        var tieNote = teams.Any(rival =>
        {
            if (rival.TeamId == team.TeamId)
                return false;
            var ceiling = rival.CurrentPoints + TitleRaceCalculator.Remaining(pending, rival.TeamId) * 3;
            return rival.CurrentPoints == maxPoints
                || ceiling == team.CurrentPoints
                || ceiling == maxPoints;
        });
        var positionNote = canPass
            ? "Ese mejor puesto solo cuenta a quienes ya superan el techo propio. Otros cruces todavía pueden dejar a un rival por encima, así que no es un puesto asegurado."
            : tieNote
                ? "Si hay igualdad de puntos, el desempate puede mover este límite. No se presenta como una certeza."
                : "El límite usa solo los puntos, con desigualdad estricta. El desempate no hace falta para sostenerlo.";
        if (!worstExact)
            positionNote += " El peor puesto suma por separado a rivales que todavía se cruzan, así que puede quedar más abajo de lo que el fixture permite.";

        var canClinch = exhaustive && CanClinch(team.TeamId, teams, pending, nextMatches);
        var clinchNote = !exhaustive
            ? "No se agotan los resultados de la próxima fecha, así que no se afirma que puedan consagrarse en esa jornada."
            : canClinch
                ? "Existe al menos un resultado de la próxima fecha que los deja primeros aunque el resto de las jornadas salga en contra."
                : "Ganar el próximo partido no alcanza, por sí solo, para quedar fuera del alcance del resto.";

        string stance;
        string label;
        string summary;
        if (team.MathematicallyCrowned)
        {
            stance = "crowned";
            label = "Consagrado";
            summary = "Ya no pueden alcanzarlos en puntos. Es una cuenta matemática, no una simulación.";
        }
        else if (team.MathematicallyEliminated)
        {
            stance = "eliminated";
            label = "Eliminado";
            summary = "Aunque ganen todo, otro equipo ya tiene más puntos de los que ellos pueden sumar.";
        }
        else if (winOutSecures)
        {
            stance = "self";
            label = "Depende de sí mismo";
            summary = "Si gana todos sus partidos restantes, asegura el primer puesto aunque el resto sume lo máximo que le queda.";
        }
        else
        {
            stance = "help";
            label = "Necesita ayuda";
            summary = "Aunque gane todo, un rival todavía puede igualarlo o superarlo. Hacen falta resultados ajenos, y eso no se puede afirmar de antemano.";
        }

        return new TitlePathReport(
            team.TeamId,
            stance,
            label,
            summary,
            team.CurrentPoints,
            maxPoints,
            teams.Count <= 1 ? 0 : rivalMaxIfIWinOut,
            clinch,
            pointsNote,
            best,
            Math.Max(best, worst),
            positionNote,
            canClinch,
            clinchNote,
            Campaigns(team.CurrentPoints, mine.Count, rivalMax, winOutSecures));
    }

    private static int WorstPosition(
        TeamOutlook team,
        IReadOnlyList<TeamOutlook> teams,
        IReadOnlyList<PredictionFixture> pending,
        out bool exact)
    {
        var threshold = team.CurrentPoints;
        var already = teams.Count(rival => rival.TeamId != team.TeamId && rival.CurrentPoints > threshold);
        var contenders = teams.Where(rival =>
            rival.TeamId != team.TeamId
            && rival.CurrentPoints <= threshold
            && rival.CurrentPoints + TitleRaceCalculator.Remaining(pending, rival.TeamId) * 3 > threshold).ToList();
        if (contenders.Count == 0)
        {
            exact = true;
            return 1 + already;
        }

        var ids = contenders.Select(item => item.TeamId).ToHashSet();
        var internalMatches = pending.Where(match =>
            match.Home.Counts && match.Away.Counts
            && ids.Contains(match.Home.TeamId)
            && ids.Contains(match.Away.TeamId)).ToList();
        if (internalMatches.Count > 10)
        {
            exact = false;
            return 1 + already + contenders.Count;
        }

        var bases = new Dictionary<Guid, int>();
        foreach (var rival in contenders)
        {
            var external = pending.Count(match =>
                TitleRaceCalculator.Remaining(new[] { match }, rival.TeamId) == 1
                && !(match.Home.Counts && match.Away.Counts
                    && ids.Contains(match.Home.TeamId)
                    && ids.Contains(match.Away.TeamId)));
            bases[rival.TeamId] = rival.CurrentPoints + external * 3;
        }

        var bestPassed = 0;
        var outcomes = 1;
        for (var i = 0; i < internalMatches.Count; i++)
            outcomes *= 3;
        for (var mask = 0; mask < outcomes; mask++)
        {
            var extra = ids.ToDictionary(id => id, _ => 0);
            var value = mask;
            foreach (var match in internalMatches)
            {
                var outcome = value % 3;
                value /= 3;
                if (outcome == 0)
                    extra[match.Home.TeamId] += 3;
                else if (outcome == 2)
                    extra[match.Away.TeamId] += 3;
                else
                {
                    extra[match.Home.TeamId] += 1;
                    extra[match.Away.TeamId] += 1;
                }
            }

            var passed = contenders.Count(rival => bases[rival.TeamId] + extra[rival.TeamId] > threshold);
            if (passed > bestPassed)
                bestPassed = passed;
        }

        exact = true;
        return 1 + already + bestPassed;
    }

    private static bool CanClinch(
        Guid teamId,
        IReadOnlyList<TeamOutlook> teams,
        IReadOnlyList<PredictionFixture> pending,
        IReadOnlyList<PredictionFixture> nextMatches)
    {
        if (nextMatches.Count == 0)
            return false;

        var later = pending.Where(match => nextMatches.All(item => item.Id != match.Id)).ToList();
        var outcomes = 1;
        for (var i = 0; i < nextMatches.Count; i++)
            outcomes *= 3;
        for (var mask = 0; mask < outcomes; mask++)
        {
            var points = teams.ToDictionary(team => team.TeamId, team => team.CurrentPoints);
            var value = mask;
            foreach (var match in nextMatches)
            {
                var outcome = value % 3;
                value /= 3;
                Add(points, match.Home, outcome == 0 ? 3 : outcome == 1 ? 1 : 0);
                Add(points, match.Away, outcome == 2 ? 3 : outcome == 1 ? 1 : 0);
            }

            var mine = points[teamId];
            var clear = true;
            foreach (var rival in teams)
            {
                if (rival.TeamId == teamId)
                    continue;
                var ceiling = points[rival.TeamId] + TitleRaceCalculator.Remaining(later, rival.TeamId) * 3;
                if (ceiling >= mine)
                {
                    clear = false;
                    break;
                }
            }

            if (clear)
                return true;
        }

        return false;
    }

    private static void Add(Dictionary<Guid, int> points, PredictionSide side, int value)
    {
        if (!side.Counts || !points.ContainsKey(side.TeamId))
            return;
        points[side.TeamId] += value;
    }

    private static List<TitleCampaign> Campaigns(int current, int remaining, int rivalMax, bool winOutSecures)
    {
        if (remaining <= 0)
            return new List<TitleCampaign>();

        if (remaining > 4)
        {
            return new List<TitleCampaign>
            {
                new(new string('G', remaining), current + remaining * 3, winOutSecures),
                new(new string('G', remaining - 1) + "P", current + (remaining - 1) * 3, current + (remaining - 1) * 3 > rivalMax),
                new(new string('E', remaining), current + remaining, current + remaining > rivalMax)
            };
        }

        var rows = new List<TitleCampaign>();
        var limit = remaining;
        for (var losses = 0; losses <= limit; losses++)
        {
            for (var draws = 0; draws <= limit - losses; draws++)
            {
                var wins = limit - losses - draws;
                var points = current + wins * 3 + draws;
                var marks = new string(Enumerable.Repeat('G', wins).Concat(Enumerable.Repeat('E', draws)).Concat(Enumerable.Repeat('P', losses)).ToArray());
                var secures = wins == limit && losses == 0 && draws == 0
                    ? winOutSecures
                    : points > rivalMax;
                rows.Add(new TitleCampaign(marks, points, secures));
            }
        }

        return rows
            .OrderByDescending(row => row.Points)
            .ThenBy(row => row.Marks)
            .Take(6)
            .ToList();
    }

    private static bool Involves(PredictionFixture match, Guid teamId) =>
        match.Home.TeamId == teamId || match.Away.TeamId == teamId;
}
