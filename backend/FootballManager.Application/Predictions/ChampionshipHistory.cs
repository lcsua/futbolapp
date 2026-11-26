namespace FootballManager.Application.Predictions;

/// <summary>
/// Rebuilds title probabilities after earlier rounds using only matches already
/// played by that cutoff. Later scores are hidden. Rows are marked reconstructed.
/// </summary>
public static class ChampionshipHistory
{
    public const int Simulations = 1_200;
    public const int MaxCutoffs = 8;

    public static ProbabilityHistory TryRebuild(PredictionDivision division, DivisionOutlook live, PredictionOptions? options = null)
    {
        options ??= PredictionModel.Default;
        if (!live.Published)
        {
            return new ProbabilityHistory(
                false,
                false,
                "No hay una predicción publicada, así que no se dibuja una evolución.",
                Array.Empty<HistoryRound>());
        }

        var completed = division.Fixtures.Where(match => match.Completed && match.HomeGoals != null && match.AwayGoals != null).ToList();
        var dated = completed.Where(match => match.Kickoff != null).ToList();
        if (dated.Count < 4 || dated.Count < completed.Count)
        {
            return new ProbabilityHistory(
                false,
                false,
                "No se reconstruye el historial: faltan las fechas de partidos ya jugados y no se inventan estados intermedios.",
                Array.Empty<HistoryRound>());
        }

        var cutoffs = new List<Cutoff>();
        foreach (var raw in dated
            .GroupBy(match => match.Round)
            .Select(group => new Cutoff(group.Key, group.Max(match => match.Kickoff!.Value)))
            .OrderBy(cutoff => cutoff.At)
            .ThenBy(cutoff => cutoff.Round))
        {
            var label = dated.Where(match => match.Kickoff <= raw.At).Max(match => match.Round);
            if (cutoffs.Count > 0 && label <= cutoffs[^1].Round)
            {
                if (raw.At > cutoffs[^1].At)
                    cutoffs[^1] = new Cutoff(cutoffs[^1].Round, raw.At);
                continue;
            }

            cutoffs.Add(new Cutoff(label, raw.At));
        }

        var eligible = new List<Cutoff>();
        foreach (var cutoff in cutoffs)
        {
            var played = dated.Count(match => match.Kickoff <= cutoff.At);
            if (played >= options.MinCompletedMatches)
                eligible.Add(cutoff);
        }

        if (eligible.Count < 2)
        {
            return new ProbabilityHistory(
                false,
                false,
                "Todavía no hay dos jornadas con fecha cierta para reconstruir la evolución.",
                Array.Empty<HistoryRound>());
        }

        var selected = eligible.TakeLast(MaxCutoffs).ToList();
        var historyOptions = options with { Simulations = Simulations, Seed = options.Seed + 17 };
        var rounds = new List<HistoryRound>();
        foreach (var cutoff in selected.Take(selected.Count - 1))
        {
            var outlook = ChampionshipPredictor.Predict(Slice(division, cutoff.At), historyOptions, includeStory: false);
            if (!outlook.Published)
                continue;
            rounds.Add(new HistoryRound(cutoff.Round, true, Points(outlook)));
        }

        var last = selected[^1];
        rounds.Add(new HistoryRound(last.Round, false, Points(live)));
        if (rounds.Count < 2)
        {
            return new ProbabilityHistory(
                false,
                false,
                "La reconstrucción no produjo una serie comparable. No se muestra un gráfico inventado.",
                Array.Empty<HistoryRound>());
        }

        return new ProbabilityHistory(
            true,
            rounds.Any(round => round.Reconstructed),
            "Los puntos anteriores a la última jornada se reconstruyen con los resultados ya jugados hasta esa fecha, sin usar lo que pasó después. El último punto es la predicción actual. La muestra reconstruida es más chica, así que un movimiento menor a dos puntos porcentuales no se muestra como un cambio.",
            rounds);
    }

    private static List<HistoryTeamPoint> Points(DivisionOutlook outlook) =>
        outlook.Teams.Select(team => new HistoryTeamPoint(
            team.TeamId,
            team.TitleProbability,
            team.PodiumProbability,
            team.ExpectedPosition,
            team.ExpectedPoints)).ToList();

    private static PredictionDivision Slice(PredictionDivision division, DateTime cutoff)
    {
        var played = new List<PredictionFixture>();
        var hidden = new List<PredictionFixture>();
        foreach (var match in division.Fixtures)
        {
            var known = match.Completed
                && match.Kickoff != null
                && match.Kickoff <= cutoff
                && match.HomeGoals != null
                && match.AwayGoals != null;
            if (known)
                played.Add(match);
            else
                hidden.Add(match with { HomeGoals = null, AwayGoals = null, Completed = false });
        }

        var table = Standings.StandingsProjector.FromResults(
            played.Select(match => new Standings.CountedResult(
                new Standings.StandingSide(
                    match.Home.TeamId,
                    match.Home.Name,
                    match.Home.Counts ? division.DivisionId : Guid.Empty,
                    match.Home.Counts ? division.DivisionName : "Externo"),
                new Standings.StandingSide(
                    match.Away.TeamId,
                    match.Away.Name,
                    match.Away.Counts ? division.DivisionId : Guid.Empty,
                    match.Away.Counts ? division.DivisionName : "Externo"),
                match.HomeGoals!.Value,
                match.AwayGoals!.Value)));

        var rows = table.Divisions.FirstOrDefault(item => item.DivisionId == division.DivisionId)?.Teams
            ?? Array.Empty<Standings.ProjectedTeam>();
        var baseline = rows.Select(team => new PredictionBaseline(
            team.TeamId,
            team.TeamName,
            team.Position,
            team.Played,
            team.GoalsFor,
            team.GoalsAgainst,
            team.Points)).ToList();

        return new PredictionDivision(division.DivisionId, division.DivisionName, baseline, played.Concat(hidden).ToList());
    }

    private readonly record struct Cutoff(int Round, DateTime At);
}
