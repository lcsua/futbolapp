using System.Diagnostics;
using System.Globalization;
using System.Text;
using FootballManager.Application.Standings;

namespace FootballManager.Application.Predictions;

public sealed record TeamOutlook(
    Guid TeamId,
    string Name,
    int OfficialPosition,
    int CurrentPoints,
    int Played,
    int GoalsFor,
    int GoalsAgainst,
    double ExpectedPoints,
    double ExpectedPosition,
    int ProjectedRank,
    int PlacesDelta,
    double TitleProbability,
    string TitleLabel,
    string TitleKind,
    bool MathematicallyCrowned,
    bool MathematicallyEliminated,
    string Form,
    IReadOnlyList<double> PositionProbability,
    double PodiumProbability = 0,
    int PointsLow = 0,
    int PointsHigh = 0,
    int PositionLow = 0,
    int PositionHigh = 0);

public sealed record MatchOutlook(
    Guid MatchId,
    int Round,
    DateTime? Kickoff,
    Guid HomeTeamId,
    string HomeName,
    Guid AwayTeamId,
    string AwayName,
    double HomeWin,
    double Draw,
    double AwayWin,
    string HomeWinLabel,
    string DrawLabel,
    string AwayWinLabel,
    int LikelyHomeGoals,
    int LikelyAwayGoals,
    string HomeForm,
    string AwayForm,
    double HomeLambda,
    double AwayLambda,
    string FavoriteLabel = "",
    string ToneLabel = "");

public sealed record DivisionOutlook(
    bool Supported,
    string? Limitation,
    bool Published,
    bool Finished,
    string? UnpublishedReason,
    string Maturity,
    string MaturityNote,
    int CompletedMatches,
    int PendingMatches,
    int PendingRounds,
    double PlayedRatio,
    int Simulations,
    long ElapsedMilliseconds,
    string ModelVersion,
    bool ChampionMeansFirstPlace,
    string ChampionNote,
    bool PromotionSupported,
    string PromotionNote,
    IReadOnlyList<TeamOutlook> Teams,
    int? NextRoundNumber,
    IReadOnlyList<MatchOutlook> NextRound,
    TitleRaceReport? Race = null,
    IReadOnlyList<TitlePathReport>? Paths = null,
    IReadOnlyList<WhatIfReport>? WhatIfs = null,
    JointWhatIfBoard? JointWhatIfs = null,
    ProbabilityHistory? History = null,
    IReadOnlyList<DecisiveMatchReport>? DecisiveMatches = null);

public static class ChampionshipPredictor
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public const string MaturityExplanation =
        "Este porcentaje mide cuánto del campeonato ya se jugó. No es una precisión estadística validada.";

    public const string ChampionExplanation =
        "Salir campeón, en este cálculo, es terminar primero. No hay playoffs ni finales cargados: si existieran, el primero de la tabla no sería automáticamente el campeón.";

    public const string PromotionExplanation =
        "No hay reglas de ascenso ni descenso cargadas para esta división, así que no se publican esas probabilidades.";

    public static DivisionOutlook Predict(
        PredictionDivision division,
        PredictionOptions? options = null,
        StandingsRules? rules = null,
        bool includeStory = true)
    {
        options ??= PredictionModel.Default;
        rules ??= StandingsRules.Official;
        var watch = Stopwatch.StartNew();
        var limitation = rules.UnsupportedReason();
        if (limitation != null)
        {
            watch.Stop();
            return Empty(division, supported: false, published: false, finished: false, limitation, null, options, watch.ElapsedMilliseconds);
        }

        var completed = division.Fixtures.Where(IsCountedResult).OrderBy(Chronology).ToList();
        var pending = division.Fixtures.Where(IsPending).OrderBy(Chronology).ToList();
        var finished = pending.Count == 0;
        var pendingRounds = pending.Select(match => match.Round).Distinct().Count();
        var ratio = completed.Count + pending.Count == 0
            ? 0
            : completed.Count / (double)(completed.Count + pending.Count);

        if (finished)
        {
            watch.Stop();
            return Empty(
                division,
                supported: true,
                published: false,
                finished: true,
                limitation: null,
                unpublished: "Este campeonato no tiene partidos pendientes. La tabla oficial es el resultado, no una predicción.",
                options,
                watch.ElapsedMilliseconds,
                completed.Count,
                pending.Count,
                pendingRounds,
                ratio);
        }

        if (completed.Count < options.MinCompletedMatches)
        {
            watch.Stop();
            return Empty(
                division,
                supported: true,
                published: false,
                finished: false,
                limitation: null,
                unpublished: $"Todavía no hay suficiente información para publicar predicciones. Hacen falta al menos {options.MinCompletedMatches} partidos finalizados en la división y hay {completed.Count}.",
                options,
                watch.ElapsedMilliseconds,
                completed.Count,
                pending.Count,
                pendingRounds,
                ratio);
        }

        var teams = BuildTeams(division, completed, pending);
        var rates = Strengths(teams, completed, options);
        var outlooks = pending.Select(match => ScoreMatch(match, rates, teams, options)).ToList();
        var nextRound = NextFixtureRound(pending);
        var next = outlooks.Where(match => match.Round == nextRound).ToList();

        var samples = Math.Max(1, options.Simulations);
        var positionHits = new int[teams.Count, teams.Count];
        var pointsSum = new double[teams.Count];
        var positionSum = new double[teams.Count];
        var pointSamples = new int[teams.Count][];
        for (var i = 0; i < teams.Count; i++)
            pointSamples[i] = new int[samples];
        var random = new Random(options.Seed);
        var pendingIndexed = pending.Select(match => (
            Home: IndexOf(teams, match.Home.TeamId),
            Away: IndexOf(teams, match.Away.TeamId),
            HomeCounts: match.Home.Counts,
            AwayCounts: match.Away.Counts,
            HomePmf: Pmf(rates.Lambda(match.Home.TeamId, match.Away.TeamId, home: true, options), options.MaxGoals),
            AwayPmf: Pmf(rates.Lambda(match.Away.TeamId, match.Home.TeamId, home: false, options), options.MaxGoals)
        )).ToList();
        var nextSlot = NextSlot(teams, pending);
        var condCount = new int[teams.Count, 3];
        var condTitle = new int[teams.Count, 3];
        var condChampion = new int[teams.Count, 3, teams.Count];

        var points = new int[teams.Count];
        var goalsFor = new int[teams.Count];
        var goalsAgainst = new int[teams.Count];
        var order = new int[teams.Count];

        for (var sample = 0; sample < samples; sample++)
        {
            for (var i = 0; i < teams.Count; i++)
            {
                points[i] = teams[i].Points;
                goalsFor[i] = teams[i].GoalsFor;
                goalsAgainst[i] = teams[i].GoalsAgainst;
                order[i] = i;
            }

            var sampledHome = new int[pendingIndexed.Count];
            var sampledAway = new int[pendingIndexed.Count];
            for (var matchIndex = 0; matchIndex < pendingIndexed.Count; matchIndex++)
            {
                var match = pendingIndexed[matchIndex];
                var homeGoals = Sample(match.HomePmf, random.NextDouble());
                var awayGoals = Sample(match.AwayPmf, random.NextDouble());
                sampledHome[matchIndex] = homeGoals;
                sampledAway[matchIndex] = awayGoals;
                if (match.HomeCounts && match.Home >= 0)
                    Apply(points, goalsFor, goalsAgainst, match.Home, homeGoals, awayGoals);
                if (match.AwayCounts && match.Away >= 0)
                    Apply(points, goalsFor, goalsAgainst, match.Away, awayGoals, homeGoals);
            }

            Array.Sort(order, (a, b) => Compare(teams, points, goalsFor, goalsAgainst, a, b));
            for (var place = 0; place < order.Length; place++)
            {
                var team = order[place];
                positionHits[team, place]++;
                pointsSum[team] += points[team];
                positionSum[team] += place + 1;
                pointSamples[team][sample] = points[team];
            }

            if (includeStory)
                NoteConditions(teams, nextSlot, pendingIndexed, sampledHome, sampledAway, order[0], condCount, condTitle, condChampion);
        }

        if (includeStory)
            TopUpConditions(teams, pendingIndexed, nextSlot, condCount, condTitle, condChampion, random);

        var rows = new List<TeamOutlook>(teams.Count);
        for (var i = 0; i < teams.Count; i++)
        {
            var distribution = new double[teams.Count];
            for (var place = 0; place < teams.Count; place++)
                distribution[place] = positionHits[i, place] / (double)samples;

            var crowned = teams[i].MathematicallyCrowned;
            var eliminated = teams[i].MathematicallyEliminated;
            // Force mathematical certainty onto the published title chance so Monte Carlo
            // noise never shows "<0,1%" for an eliminated side or less than 100% for a crowned one.
            if (crowned)
            {
                distribution[0] = 1;
                for (var place = 1; place < distribution.Length; place++)
                    distribution[place] = 0;
            }
            else if (eliminated)
            {
                var first = distribution[0];
                distribution[0] = 0;
                if (first > 0 && distribution.Length > 1)
                {
                    var rest = distribution.Skip(1).Sum();
                    if (rest > 0)
                    {
                        for (var place = 1; place < distribution.Length; place++)
                            distribution[place] += first * (distribution[place] / rest);
                    }
                    else if (distribution.Length > 1)
                    {
                        distribution[1] = 1;
                    }
                }
            }

            var title = crowned ? 1d : eliminated ? 0d : distribution[0];
            var (label, kind) = TitleLabel(title, crowned, eliminated);
            // Keep expected place aligned with the adjusted distribution (crowned/eliminated).
            var expectedPosition = 0d;
            for (var place = 0; place < distribution.Length; place++)
                expectedPosition += (place + 1) * distribution[place];
            if (expectedPosition <= 0)
                expectedPosition = positionSum[i] / samples;
            var official = teams[i].OfficialPosition;

            rows.Add(new TeamOutlook(
                teams[i].Id,
                teams[i].Name,
                official,
                teams[i].Points,
                teams[i].Played,
                teams[i].GoalsFor,
                teams[i].GoalsAgainst,
                pointsSum[i] / samples,
                expectedPosition,
                0,
                0,
                title,
                label,
                kind,
                crowned,
                eliminated,
                teams[i].Form,
                distribution));
        }

        var ranked = rows
            .OrderBy(team => team.ExpectedPosition)
            .ThenByDescending(team => team.ExpectedPoints)
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) =>
            {
                var rank = index + 1;
                var delta = team.OfficialPosition > 0 ? team.OfficialPosition - rank : 0;
                var slot = IndexOf(teams, team.TeamId);
                var distribution = team.PositionProbability;
                var podium = 0d;
                for (var place = 0; place < Math.Min(3, distribution.Count); place++)
                    podium += distribution[place];
                return team with
                {
                    ProjectedRank = rank,
                    PlacesDelta = delta,
                    PodiumProbability = podium,
                    PointsLow = slot < 0 ? team.CurrentPoints : Percentile(pointSamples[slot], 0.10),
                    PointsHigh = slot < 0 ? team.CurrentPoints : Percentile(pointSamples[slot], 0.90),
                    PositionLow = PositionPercentile(distribution, 0.10),
                    PositionHigh = PositionPercentile(distribution, 0.90)
                };
            })
            .ToList();

        next = DecorateMatches(next, ranked).ToList();
        var race = includeStory ? TitleRaceCalculator.Build(ranked, pending) : null;
        var paths = includeStory ? TitlePathCalculator.Build(ranked, pending) : null;
        var whatIfs = includeStory ? WhatIfs(teams, ranked, pending, nextSlot, condCount, condTitle, condChampion) : null;
        JointWhatIfBoard? jointWhatIfs = null;
        if (includeStory)
        {
            var focus = FocusIndices(teams, ranked, nextSlot, race);
            var pairs = BuildPairList(focus);
            if (pairs.Count > 0)
            {
                var jointCount = new int[pairs.Count, 3, 3];
                var jointTitleLeft = new int[pairs.Count, 3, 3];
                var jointTitleRight = new int[pairs.Count, 3, 3];
                var jointChampion = new int[pairs.Count, 3, 3, teams.Count];
                TopUpJointConditions(teams, pendingIndexed, nextSlot, pairs, jointCount, jointTitleLeft, jointTitleRight, jointChampion, random);
                jointWhatIfs = BuildJointWhatIfs(teams, ranked, pending, nextSlot, race, pairs, jointCount, jointTitleLeft, jointTitleRight, jointChampion);
            }
        }
        var decisive = includeStory ? DecisiveMatchCalculator.Build(next, ranked, whatIfs) : null;

        watch.Stop();
        return new DivisionOutlook(
            Supported: true,
            Limitation: null,
            Published: true,
            Finished: false,
            UnpublishedReason: null,
            Maturity: MaturityOf(ratio),
            MaturityNote: MaturityExplanation,
            CompletedMatches: completed.Count,
            PendingMatches: pending.Count,
            PendingRounds: pendingRounds,
            PlayedRatio: ratio,
            Simulations: samples,
            ElapsedMilliseconds: watch.ElapsedMilliseconds,
            ModelVersion: PredictionModel.Version,
            ChampionMeansFirstPlace: true,
            ChampionNote: ChampionExplanation,
            PromotionSupported: false,
            PromotionNote: PromotionExplanation,
            Teams: ranked,
            NextRoundNumber: nextRound,
            NextRound: next,
            Race: race,
            Paths: paths,
            WhatIfs: whatIfs,
            JointWhatIfs: jointWhatIfs,
            DecisiveMatches: decisive);
    }

    /// <summary>
    /// Ranks one hypothetical sample with the same tie-break as <see cref="StandingsProjector.FromBaseline"/>.
    /// Tests use it to keep the Monte Carlo path honest.
    /// </summary>
    public static IReadOnlyList<ProjectedTeam> ProjectSample(
        PredictionDivision division,
        IReadOnlyList<SimulatedScore> scores,
        StandingsRules? rules = null)
    {
        var baseline = division.Baseline.Select(team => new BaselineTeam(
            team.TeamId,
            team.Name,
            team.Position,
            team.Played,
            0,
            0,
            0,
            team.GoalsFor,
            team.GoalsAgainst,
            team.Points));
        return StandingsProjector
            .FromBaseline(division.DivisionId, division.DivisionName, baseline, scores, rules: rules)
            .Divisions[0].Teams;
    }

    public static IReadOnlyList<MatchOutlook> EstimateUpcoming(
        PredictionDivision division,
        IReadOnlyList<PredictionFixture> upcoming,
        PredictionOptions? options = null)
    {
        options ??= PredictionModel.Default;
        var completed = division.Fixtures.Where(IsCountedResult).OrderBy(Chronology).ToList();
        var teams = BuildTeams(division, completed, upcoming);
        var rates = Strengths(teams, completed, options);
        return upcoming.Select(match => ScoreMatch(match, rates, teams, options)).ToList();
    }

    public static string FormatChance(double probability) => PredictionChance.Format(probability);

    /// <summary>One sample through the Monte Carlo ranker, for comparison with the official projector.</summary>
    public static IReadOnlyList<(Guid TeamId, int Position, int Points, int GoalsFor, int GoalsAgainst)> RankFast(
        PredictionDivision division,
        IReadOnlyList<SimulatedScore> scores)
    {
        var completed = division.Fixtures.Where(IsCountedResult).OrderBy(Chronology).ToList();
        var teams = BuildTeams(division, completed, Array.Empty<PredictionFixture>());
        var points = teams.Select(team => team.Points).ToArray();
        var goalsFor = teams.Select(team => team.GoalsFor).ToArray();
        var goalsAgainst = teams.Select(team => team.GoalsAgainst).ToArray();
        foreach (var score in scores)
        {
            if (!StandingsProjector.TryReadScore(score.HomeGoals, score.AwayGoals, out var homeGoals, out var awayGoals))
                continue;
            var home = IndexOf(teams, score.HomeTeamId);
            var away = IndexOf(teams, score.AwayTeamId);
            if (score.HomeCounts && home >= 0)
                Apply(points, goalsFor, goalsAgainst, home, homeGoals, awayGoals);
            if (score.AwayCounts && away >= 0)
                Apply(points, goalsFor, goalsAgainst, away, awayGoals, homeGoals);
        }

        var order = Enumerable.Range(0, teams.Count).ToArray();
        Array.Sort(order, (a, b) => Compare(teams, points, goalsFor, goalsAgainst, a, b));
        return order.Select((index, place) => (
            teams[index].Id,
            place + 1,
            points[index],
            goalsFor[index],
            goalsAgainst[index])).ToList();
    }

    private static (string Label, string Kind) TitleLabel(double probability, bool crowned, bool eliminated)
    {
        if (crowned)
            return ("Consagrado", "certain");
        if (eliminated)
            return ("Eliminado", "eliminated");
        if (probability <= 0)
            return ("Sin muestra", "unseen");
        if (probability * 100 < 0.1)
            return ("<0,1%", "below");
        if (probability >= 1)
            return ("100%", "certain");
        if (probability * 100 > 99.9)
            return (">99,9%", "above");
        return (FormatChance(probability), "chance");
    }

    private static string MaturityOf(double ratio)
    {
        if (ratio < 0.25) return "Temprana";
        if (ratio < 0.60) return "Intermedia";
        return "Avanzada";
    }

    private static DivisionOutlook Empty(
        PredictionDivision division,
        bool supported,
        bool published,
        bool finished,
        string? limitation,
        string? unpublished,
        PredictionOptions options,
        long elapsed,
        int completed = 0,
        int pending = 0,
        int pendingRounds = 0,
        double ratio = 0)
    {
        var teams = division.Baseline
            .OrderBy(team => team.Position <= 0 ? int.MaxValue : team.Position)
            .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
            .Select((team, index) => new TeamOutlook(
                team.TeamId,
                team.Name,
                team.Position,
                team.Points,
                team.Played,
                team.GoalsFor,
                team.GoalsAgainst,
                team.Points,
                team.Position,
                index + 1,
                0,
                0,
                string.Empty,
                "hidden",
                false,
                false,
                string.Empty,
                Array.Empty<double>()))
            .ToList();

        return new DivisionOutlook(
            supported,
            limitation,
            published,
            finished,
            unpublished,
            finished ? "Cerrado" : MaturityOf(ratio),
            MaturityExplanation,
            completed,
            pending,
            pendingRounds,
            ratio,
            0,
            elapsed,
            PredictionModel.Version,
            true,
            ChampionExplanation,
            false,
            PromotionExplanation,
            teams,
            null,
            Array.Empty<MatchOutlook>());
    }

    /// <summary>
    /// The public "next round" is the earliest pending round that is still ahead.
    /// A suspended or postponed match from an earlier date stays in the simulation,
    /// but it does not replace the upcoming jornada.
    /// </summary>
    internal static int NextFixtureRound(IReadOnlyList<PredictionFixture> pending)
    {
        var today = ArgentinaClock.Today();
        var upcoming = pending
            .Where(match => match.Kickoff == null || match.Kickoff.Value.Date >= today)
            .ToList();
        var pool = upcoming.Count > 0 ? upcoming : pending;
        return pool.Min(match => match.Round);
    }

    private static List<TeamState> BuildTeams(
        PredictionDivision division,
        IReadOnlyList<PredictionFixture> completed,
        IReadOnlyList<PredictionFixture> pending)
    {
        var map = new Dictionary<Guid, TeamState>();
        foreach (var team in division.Baseline)
        {
            map[team.TeamId] = new TeamState
            {
                Id = team.TeamId,
                Name = string.IsNullOrWhiteSpace(team.Name) ? "Equipo" : team.Name,
                OfficialPosition = team.Position,
                Seen = team.Position > 0 ? team.Position : int.MaxValue,
                Played = team.Played,
                GoalsFor = team.GoalsFor,
                GoalsAgainst = team.GoalsAgainst,
                Points = team.Points
            };
        }

        void Touch(PredictionSide side)
        {
            if (!side.Counts || side.TeamId == Guid.Empty)
                return;
            if (map.ContainsKey(side.TeamId))
            {
                if (!string.IsNullOrWhiteSpace(side.Name))
                    map[side.TeamId].Name = side.Name;
                return;
            }

            map[side.TeamId] = new TeamState
            {
                Id = side.TeamId,
                Name = string.IsNullOrWhiteSpace(side.Name) ? "Equipo" : side.Name,
                Seen = int.MaxValue
            };
        }

        foreach (var match in completed.Concat(pending))
        {
            Touch(match.Home);
            Touch(match.Away);
        }

        var form = new Dictionary<Guid, List<char>>();
        foreach (var match in completed)
        {
            NoteForm(form, match.Home, match.HomeGoals!.Value, match.AwayGoals!.Value);
            NoteForm(form, match.Away, match.AwayGoals!.Value, match.HomeGoals!.Value);
        }

        foreach (var team in map.Values)
        {
            if (form.TryGetValue(team.Id, out var marks) && marks.Count > 0)
            {
                var last = marks.Skip(Math.Max(0, marks.Count - PredictionModel.RecentWindow));
                team.Form = string.Join(' ', last);
            }
            else
            {
                team.Form = "Sin partidos";
            }
        }

        var pendingLeft = new Dictionary<Guid, int>();
        foreach (var match in pending)
        {
            if (match.Home.Counts)
                pendingLeft[match.Home.TeamId] = pendingLeft.GetValueOrDefault(match.Home.TeamId) + 1;
            if (match.Away.Counts)
                pendingLeft[match.Away.TeamId] = pendingLeft.GetValueOrDefault(match.Away.TeamId) + 1;
        }

        foreach (var team in map.Values)
            team.Remaining = pendingLeft.GetValueOrDefault(team.Id);

        var list = map.Values.OrderBy(team => team.Id).ToList();
        for (var i = 0; i < list.Count; i++)
        {
            var mine = list[i].Points + 3 * list[i].Remaining;
            var crowned = true;
            var eliminated = false;
            for (var j = 0; j < list.Count; j++)
            {
                if (i == j) continue;
                var otherMax = list[j].Points + 3 * list[j].Remaining;
                if (otherMax >= list[i].Points)
                    crowned = false;
                if (list[j].Points > mine)
                    eliminated = true;
            }

            if (list.Count < 2)
                crowned = false;
            list[i].MathematicallyCrowned = crowned;
            list[i].MathematicallyEliminated = eliminated && !crowned;
        }

        return list;
    }

    private static void NoteForm(Dictionary<Guid, List<char>> form, PredictionSide side, int goalsFor, int goalsAgainst)
    {
        if (!side.Counts || side.TeamId == Guid.Empty)
            return;
        if (!form.TryGetValue(side.TeamId, out var marks))
        {
            marks = new List<char>();
            form[side.TeamId] = marks;
        }

        marks.Add(goalsFor > goalsAgainst ? 'G' : goalsFor < goalsAgainst ? 'P' : 'E');
    }

    private static Rates Strengths(List<TeamState> teams, IReadOnlyList<PredictionFixture> completed, PredictionOptions options)
    {
        var appearances = new Dictionary<Guid, List<Appearance>>();
        foreach (var team in teams)
            appearances[team.Id] = new List<Appearance>();

        foreach (var match in completed)
        {
            if (match.Home.Counts && appearances.ContainsKey(match.Home.TeamId))
            {
                appearances[match.Home.TeamId].Add(new Appearance(
                    match.Away.TeamId,
                    match.Away.TeamId != Guid.Empty && appearances.ContainsKey(match.Away.TeamId),
                    match.HomeGoals!.Value,
                    match.AwayGoals!.Value));
            }

            if (match.Away.Counts && appearances.ContainsKey(match.Away.TeamId))
            {
                appearances[match.Away.TeamId].Add(new Appearance(
                    match.Home.TeamId,
                    match.Home.TeamId != Guid.Empty && appearances.ContainsKey(match.Home.TeamId),
                    match.AwayGoals!.Value,
                    match.HomeGoals!.Value));
            }
        }

        var totalGoals = 0;
        var totalAppearances = 0;
        foreach (var list in appearances.Values)
        {
            totalAppearances += list.Count;
            totalGoals += list.Sum(item => item.GoalsFor);
        }

        var mu = totalAppearances == 0 ? options.GoalsPrior : totalGoals / (double)totalAppearances;
        if (mu < 0.05) mu = options.GoalsPrior;

        var attackRate = new Dictionary<Guid, double>();
        var defenseRate = new Dictionary<Guid, double>();
        foreach (var team in teams)
        {
            var (attack, defense) = Blended(appearances[team.Id], options, mu, adjust: false, attackRate, defenseRate);
            attackRate[team.Id] = attack;
            defenseRate[team.Id] = defense;
        }

        var attackStrength = new Dictionary<Guid, double>();
        var defenseStrength = new Dictionary<Guid, double>();
        foreach (var team in teams)
        {
            var (attack, defense) = Blended(appearances[team.Id], options, mu, adjust: true, attackRate, defenseRate);
            attackStrength[team.Id] = Clamp(attack / mu, PredictionModel.MinStrength, PredictionModel.MaxStrength);
            defenseStrength[team.Id] = Clamp(defense / mu, PredictionModel.MinStrength, PredictionModel.MaxStrength);
        }

        return new Rates(mu, attackStrength, defenseStrength);
    }

    private static (double Attack, double Defense) Blended(
        List<Appearance> matches,
        PredictionOptions options,
        double mu,
        bool adjust,
        Dictionary<Guid, double> attackRate,
        Dictionary<Guid, double> defenseRate)
    {
        if (matches.Count == 0)
            return (mu, mu);

        double allGf = 0, allGa = 0, allW = 0, recentGf = 0, recentGa = 0, recentW = 0;
        var start = Math.Max(0, matches.Count - Math.Max(1, options.RecentWindow));
        for (var i = 0; i < matches.Count; i++)
        {
            var weight = Math.Pow(options.RecencyDecay, matches.Count - 1 - i);
            var item = matches[i];
            double gf = item.GoalsFor;
            double ga = item.GoalsAgainst;
            if (adjust)
            {
                var oppDefense = mu;
                if (item.OpponentKnown && defenseRate.TryGetValue(item.OpponentId, out var knownDefense))
                    oppDefense = Math.Max(knownDefense, mu * 0.25);
                var oppAttack = mu;
                if (item.OpponentKnown && attackRate.TryGetValue(item.OpponentId, out var knownAttack))
                    oppAttack = Math.Max(knownAttack, mu * 0.25);
                gf = item.GoalsFor * mu / oppDefense;
                ga = item.GoalsAgainst * mu / oppAttack;
            }

            allGf += weight * gf;
            allGa += weight * ga;
            allW += weight;
            if (i >= start)
            {
                recentGf += weight * gf;
                recentGa += weight * ga;
                recentW += weight;
            }
        }

        var seasonGf = allGf / allW;
        var seasonGa = allGa / allW;
        var windowGf = recentW > 0 ? recentGf / recentW : seasonGf;
        var windowGa = recentW > 0 ? recentGa / recentW : seasonGa;
        var blendGf = options.RecentBlend * windowGf + (1 - options.RecentBlend) * seasonGf;
        var blendGa = options.RecentBlend * windowGa + (1 - options.RecentBlend) * seasonGa;
        var n = matches.Count;
        var prior = Math.Max(0, options.PriorMatches);
        var attack = (n * blendGf + prior * mu) / (n + prior);
        var defense = (n * blendGa + prior * mu) / (n + prior);
        return (attack, defense);
    }

    private static MatchOutlook ScoreMatch(
        PredictionFixture match,
        Rates rates,
        List<TeamState> teams,
        PredictionOptions options)
    {
        var homeLambda = rates.Lambda(match.Home.TeamId, match.Away.TeamId, home: true, options);
        var awayLambda = rates.Lambda(match.Away.TeamId, match.Home.TeamId, home: false, options);
        var homePmf = Pmf(homeLambda, options.MaxGoals);
        var awayPmf = Pmf(awayLambda, options.MaxGoals);
        double home = 0, draw = 0, away = 0, best = -1;
        var likelyHome = 0;
        var likelyAway = 0;
        for (var hg = 0; hg < homePmf.Length; hg++)
        {
            for (var ag = 0; ag < awayPmf.Length; ag++)
            {
                var probability = homePmf[hg] * awayPmf[ag];
                if (hg > ag) home += probability;
                else if (hg == ag) draw += probability;
                else away += probability;
                if (probability > best)
                {
                    best = probability;
                    likelyHome = hg;
                    likelyAway = ag;
                }
            }
        }

        var homeState = teams.FirstOrDefault(team => team.Id == match.Home.TeamId);
        var awayState = teams.FirstOrDefault(team => team.Id == match.Away.TeamId);
        return new MatchOutlook(
            match.Id,
            match.Round,
            match.Kickoff,
            match.Home.TeamId,
            match.Home.Name,
            match.Away.TeamId,
            match.Away.Name,
            home,
            draw,
            away,
            FormatChance(home),
            FormatChance(draw),
            FormatChance(away),
            likelyHome,
            likelyAway,
            homeState?.Form ?? "Sin partidos",
            awayState?.Form ?? "Sin partidos",
            homeLambda,
            awayLambda);
    }

    private static int[] NextSlot(List<TeamState> teams, IReadOnlyList<PredictionFixture> pending)
    {
        var slots = new int[teams.Count];
        Array.Fill(slots, -1);
        var today = ArgentinaClock.Today();
        var rank = new int[teams.Count];
        var round = new int[teams.Count];
        Array.Fill(rank, int.MaxValue);
        Array.Fill(round, int.MaxValue);

        for (var index = 0; index < pending.Count; index++)
        {
            var match = pending[index];
            var upcoming = match.Kickoff == null || match.Kickoff.Value.Date >= today;
            var matchRank = upcoming ? 0 : 1;
            void Consider(PredictionSide side)
            {
                if (!side.Counts)
                    return;
                var team = IndexOf(teams, side.TeamId);
                if (team < 0)
                    return;
                var better = matchRank < rank[team]
                    || (matchRank == rank[team] && match.Round < round[team]);
                if (!better)
                    return;
                slots[team] = index;
                rank[team] = matchRank;
                round[team] = match.Round;
            }

            Consider(match.Home);
            Consider(match.Away);
        }

        return slots;
    }

    private static void NoteConditions(
        List<TeamState> teams,
        int[] nextSlot,
        IReadOnlyList<(int Home, int Away, bool HomeCounts, bool AwayCounts, double[] HomePmf, double[] AwayPmf)> pending,
        int[] homeGoals,
        int[] awayGoals,
        int champion,
        int[,] condCount,
        int[,] condTitle,
        int[,,] condChampion)
    {
        for (var team = 0; team < teams.Count; team++)
        {
            var slot = nextSlot[team];
            if (slot < 0 || slot >= pending.Count)
                continue;
            var match = pending[slot];
            int outcome;
            if (match.Home == team)
                outcome = homeGoals[slot] == awayGoals[slot] ? 1 : homeGoals[slot] > awayGoals[slot] ? 0 : 2;
            else if (match.Away == team)
                outcome = homeGoals[slot] == awayGoals[slot] ? 1 : awayGoals[slot] > homeGoals[slot] ? 0 : 2;
            else
                continue;

            condCount[team, outcome]++;
            if (champion == team)
                condTitle[team, outcome]++;
            if (champion >= 0)
                condChampion[team, outcome, champion]++;
        }
    }

    private static void TopUpConditions(
        List<TeamState> teams,
        IReadOnlyList<(int Home, int Away, bool HomeCounts, bool AwayCounts, double[] HomePmf, double[] AwayPmf)> pendingIndexed,
        int[] nextSlot,
        int[,] condCount,
        int[,] condTitle,
        int[,,] condChampion,
        Random random)
    {
        const int minimum = 200;
        const int extra = 800;
        for (var team = 0; team < teams.Count; team++)
        {
            var slot = nextSlot[team];
            if (slot < 0)
                continue;
            for (var outcome = 0; outcome < 3; outcome++)
            {
                if (condCount[team, outcome] >= minimum)
                    continue;
                var table = ConditionalScores(pendingIndexed[slot].HomePmf, pendingIndexed[slot].AwayPmf, pendingIndexed[slot].Home == team, outcome);
                if (table.Pairs.Length == 0)
                    continue;
                for (var sample = 0; sample < extra; sample++)
                {
                    var forced = table.Pairs[SampleCumulative(table.Cumulative, random.NextDouble())];
                    var champion = Play(teams, pendingIndexed, slot, forced.Home, forced.Away, random);
                    NoteForced(team, outcome, champion, condCount, condTitle, condChampion);
                    var match = pendingIndexed[slot];
                    var opponent = match.Home == team ? match.Away : match.Home;
                    var mirrored = outcome == 1 ? 1 : outcome == 0 ? 2 : 0;
                    if (opponent >= 0 && opponent != team && nextSlot[opponent] == slot)
                        NoteForced(opponent, mirrored, champion, condCount, condTitle, condChampion);
                }
            }
        }
    }

    private static void NoteForced(
        int team,
        int outcome,
        int champion,
        int[,] condCount,
        int[,] condTitle,
        int[,,] condChampion)
    {
        condCount[team, outcome]++;
        if (champion == team)
            condTitle[team, outcome]++;
        if (champion >= 0)
            condChampion[team, outcome, champion]++;
    }

    private static int Play(
        List<TeamState> teams,
        IReadOnlyList<(int Home, int Away, bool HomeCounts, bool AwayCounts, double[] HomePmf, double[] AwayPmf)> pending,
        int forcedSlot,
        int forcedHome,
        int forcedAway,
        Random random) =>
        Play(teams, pending, new Dictionary<int, ForcedScore> { [forcedSlot] = new(forcedHome, forcedAway) }, random);

    private static int Play(
        List<TeamState> teams,
        IReadOnlyList<(int Home, int Away, bool HomeCounts, bool AwayCounts, double[] HomePmf, double[] AwayPmf)> pending,
        IReadOnlyDictionary<int, ForcedScore> forced,
        Random random)
    {
        var points = new int[teams.Count];
        var goalsFor = new int[teams.Count];
        var goalsAgainst = new int[teams.Count];
        var order = new int[teams.Count];
        for (var i = 0; i < teams.Count; i++)
        {
            points[i] = teams[i].Points;
            goalsFor[i] = teams[i].GoalsFor;
            goalsAgainst[i] = teams[i].GoalsAgainst;
            order[i] = i;
        }

        for (var index = 0; index < pending.Count; index++)
        {
            var match = pending[index];
            int homeGoals;
            int awayGoals;
            if (forced.TryGetValue(index, out var score))
            {
                homeGoals = score.Home;
                awayGoals = score.Away;
            }
            else
            {
                homeGoals = Sample(match.HomePmf, random.NextDouble());
                awayGoals = Sample(match.AwayPmf, random.NextDouble());
            }

            if (match.HomeCounts && match.Home >= 0)
                Apply(points, goalsFor, goalsAgainst, match.Home, homeGoals, awayGoals);
            if (match.AwayCounts && match.Away >= 0)
                Apply(points, goalsFor, goalsAgainst, match.Away, awayGoals, homeGoals);
        }

        Array.Sort(order, (a, b) => Compare(teams, points, goalsFor, goalsAgainst, a, b));
        return order[0];
    }

    private readonly record struct ForcedScore(int Home, int Away);

    private static (double[] Cumulative, ForcedScore[] Pairs) ConditionalScores(double[] homePmf, double[] awayPmf, bool teamIsHome, int wanted)
    {
        var pairs = new List<ForcedScore>();
        var weights = new List<double>();
        double total = 0;
        for (var home = 0; home < homePmf.Length; home++)
        {
            for (var away = 0; away < awayPmf.Length; away++)
            {
                var probability = homePmf[home] * awayPmf[away];
                if (probability <= 0)
                    continue;
                var teamGoals = teamIsHome ? home : away;
                var otherGoals = teamIsHome ? away : home;
                var outcome = teamGoals > otherGoals ? 0 : teamGoals == otherGoals ? 1 : 2;
                if (outcome != wanted)
                    continue;
                pairs.Add(new ForcedScore(home, away));
                weights.Add(probability);
                total += probability;
            }
        }

        var cumulative = new double[weights.Count];
        double running = 0;
        for (var i = 0; i < weights.Count; i++)
        {
            running += total <= 0 ? 0 : weights[i] / total;
            cumulative[i] = running;
        }

        return (cumulative, pairs.ToArray());
    }

    private static int SampleCumulative(double[] cumulative, double draw)
    {
        for (var i = 0; i < cumulative.Length; i++)
        {
            if (draw <= cumulative[i])
                return i;
        }

        return Math.Max(0, cumulative.Length - 1);
    }

    private static List<WhatIfReport> WhatIfs(
        List<TeamState> teams,
        IReadOnlyList<TeamOutlook> ranked,
        IReadOnlyList<PredictionFixture> pending,
        int[] nextSlot,
        int[,] condCount,
        int[,] condTitle,
        int[,,] condChampion)
    {
        var reports = new List<WhatIfReport>();
        foreach (var team in ranked)
        {
            var index = IndexOf(teams, team.TeamId);
            if (index < 0 || nextSlot[index] < 0 || nextSlot[index] >= pending.Count)
                continue;
            var match = pending[nextSlot[index]];
            var opponent = match.Home.TeamId == team.TeamId ? match.Away.Name : match.Home.Name;
            var rivals = ranked
                .Where(other => other.TeamId != team.TeamId)
                .OrderByDescending(other => other.TitleProbability)
                .Take(2)
                .Select(other =>
                {
                    var otherIndex = IndexOf(teams, other.TeamId);
                    return new WhatIfRival(
                        other.TeamId,
                        other.Name,
                        Chance(condChampion, index, 0, otherIndex, condCount),
                        Chance(condChampion, index, 1, otherIndex, condCount),
                        Chance(condChampion, index, 2, otherIndex, condCount));
                })
                .ToList();
            reports.Add(new WhatIfReport(
                team.TeamId,
                match.Id,
                match.Round,
                opponent,
                Chance(condTitle, index, 0, condCount),
                Chance(condTitle, index, 1, condCount),
                Chance(condTitle, index, 2, condCount),
                condCount[index, 0] >= 200,
                condCount[index, 1] >= 200,
                condCount[index, 2] >= 200,
                condCount[index, 0],
                condCount[index, 1],
                condCount[index, 2],
                rivals));
        }

        return reports;
    }

    private static double Chance(int[,] titles, int team, int outcome, int[,] counts)
    {
        var samples = counts[team, outcome];
        return samples <= 0 ? 0 : titles[team, outcome] / (double)samples;
    }

    private static double Chance(int[,,] titles, int team, int outcome, int rival, int[,] counts)
    {
        if (rival < 0)
            return 0;
        var samples = counts[team, outcome];
        return samples <= 0 ? 0 : titles[team, outcome, rival] / (double)samples;
    }

    private static int[] FocusIndices(
        List<TeamState> teams,
        IReadOnlyList<TeamOutlook> ranked,
        int[] nextSlot,
        TitleRaceReport? race)
    {
        var selected = new List<int>();
        void TryAdd(Guid teamId)
        {
            var index = IndexOf(teams, teamId);
            if (index < 0 || nextSlot[index] < 0 || selected.Contains(index))
                return;
            selected.Add(index);
        }

        if (race != null)
        {
            foreach (var id in race.CandidateIds)
                TryAdd(id);
        }

        foreach (var team in ranked.OrderByDescending(item => item.TitleProbability))
        {
            if (selected.Count >= 4)
                break;
            if (team.MathematicallyEliminated)
                continue;
            if (selected.Count >= 2 && team.TitleProbability < 0.02)
                continue;
            TryAdd(team.TeamId);
        }

        return selected.ToArray();
    }

    private static List<(int Left, int Right)> BuildPairList(int[] focus)
    {
        var pairs = new List<(int Left, int Right)>();
        for (var i = 0; i < focus.Length; i++)
        {
            for (var j = i + 1; j < focus.Length; j++)
                pairs.Add((focus[i], focus[j]));
        }

        return pairs;
    }

    private static void TopUpJointConditions(
        List<TeamState> teams,
        IReadOnlyList<(int Home, int Away, bool HomeCounts, bool AwayCounts, double[] HomePmf, double[] AwayPmf)> pendingIndexed,
        int[] nextSlot,
        IReadOnlyList<(int Left, int Right)> pairs,
        int[,,] jointCount,
        int[,,] jointTitleLeft,
        int[,,] jointTitleRight,
        int[,,,] jointChampion,
        Random random)
    {
        const int minimum = 200;
        const int extra = 400;
        for (var pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
        {
            var (left, right) = pairs[pairIndex];
            var slotLeft = nextSlot[left];
            var slotRight = nextSlot[right];
            if (slotLeft < 0 || slotRight < 0)
                continue;
            var headToHead = slotLeft == slotRight;
            for (var outcomeLeft = 0; outcomeLeft < 3; outcomeLeft++)
            {
                for (var outcomeRight = 0; outcomeRight < 3; outcomeRight++)
                {
                    if (headToHead && !MirroredOutcomes(outcomeLeft, outcomeRight))
                        continue;
                    if (jointCount[pairIndex, outcomeLeft, outcomeRight] >= minimum)
                        continue;

                    if (headToHead)
                    {
                        var table = ConditionalScores(
                            pendingIndexed[slotLeft].HomePmf,
                            pendingIndexed[slotLeft].AwayPmf,
                            pendingIndexed[slotLeft].Home == left,
                            outcomeLeft);
                        if (table.Pairs.Length == 0)
                            continue;
                        for (var sample = 0; sample < extra; sample++)
                        {
                            var forced = table.Pairs[SampleCumulative(table.Cumulative, random.NextDouble())];
                            var champion = Play(teams, pendingIndexed, slotLeft, forced.Home, forced.Away, random);
                            NoteJointForced(pairIndex, outcomeLeft, outcomeRight, left, right, champion, jointCount, jointTitleLeft, jointTitleRight, jointChampion);
                        }
                    }
                    else
                    {
                        var tableLeft = ConditionalScores(
                            pendingIndexed[slotLeft].HomePmf,
                            pendingIndexed[slotLeft].AwayPmf,
                            pendingIndexed[slotLeft].Home == left,
                            outcomeLeft);
                        var tableRight = ConditionalScores(
                            pendingIndexed[slotRight].HomePmf,
                            pendingIndexed[slotRight].AwayPmf,
                            pendingIndexed[slotRight].Home == right,
                            outcomeRight);
                        if (tableLeft.Pairs.Length == 0 || tableRight.Pairs.Length == 0)
                            continue;
                        for (var sample = 0; sample < extra; sample++)
                        {
                            var forcedLeft = tableLeft.Pairs[SampleCumulative(tableLeft.Cumulative, random.NextDouble())];
                            var forcedRight = tableRight.Pairs[SampleCumulative(tableRight.Cumulative, random.NextDouble())];
                            var forced = new Dictionary<int, ForcedScore>
                            {
                                [slotLeft] = forcedLeft,
                                [slotRight] = forcedRight
                            };
                            var champion = Play(teams, pendingIndexed, forced, random);
                            NoteJointForced(pairIndex, outcomeLeft, outcomeRight, left, right, champion, jointCount, jointTitleLeft, jointTitleRight, jointChampion);
                        }
                    }
                }
            }
        }
    }

    private static bool MirroredOutcomes(int left, int right) =>
        left == 1 ? right == 1 : left + right == 2;

    private static void NoteJointForced(
        int pairIndex,
        int outcomeLeft,
        int outcomeRight,
        int left,
        int right,
        int champion,
        int[,,] jointCount,
        int[,,] jointTitleLeft,
        int[,,] jointTitleRight,
        int[,,,] jointChampion)
    {
        jointCount[pairIndex, outcomeLeft, outcomeRight]++;
        if (champion == left)
            jointTitleLeft[pairIndex, outcomeLeft, outcomeRight]++;
        if (champion == right)
            jointTitleRight[pairIndex, outcomeLeft, outcomeRight]++;
        if (champion >= 0)
            jointChampion[pairIndex, outcomeLeft, outcomeRight, champion]++;
    }

    private static JointWhatIfBoard? BuildJointWhatIfs(
        List<TeamState> teams,
        IReadOnlyList<TeamOutlook> ranked,
        IReadOnlyList<PredictionFixture> pending,
        int[] nextSlot,
        TitleRaceReport? race,
        IReadOnlyList<(int Left, int Right)> pairs,
        int[,,] jointCount,
        int[,,] jointTitleLeft,
        int[,,] jointTitleRight,
        int[,,,] jointChampion)
    {
        var options = new List<JointWhatIfTeamOption>();
        foreach (var team in ranked.OrderByDescending(item => item.TitleProbability))
        {
            var index = IndexOf(teams, team.TeamId);
            if (index < 0 || nextSlot[index] < 0 || nextSlot[index] >= pending.Count)
                continue;
            var match = pending[nextSlot[index]];
            var isHome = match.Home.TeamId == team.TeamId;
            var opponent = isHome ? match.Away.Name : match.Home.Name;
            options.Add(new JointWhatIfTeamOption(
                team.TeamId,
                team.Name,
                match.Id,
                match.Round,
                opponent,
                isHome,
                team.CurrentPoints,
                team.TitleProbability));
        }

        if (options.Count < 2)
            return null;

        var otherCandidates = ranked
            .Where(team => !team.MathematicallyEliminated && team.TitleProbability >= 0.01)
            .OrderByDescending(team => team.TitleProbability)
            .Take(5)
            .ToList();

        var pairReports = new List<JointWhatIfPair>();
        for (var pairIndex = 0; pairIndex < pairs.Count; pairIndex++)
        {
            var (left, right) = pairs[pairIndex];
            var headToHead = nextSlot[left] == nextSlot[right];
            var cells = new List<JointWhatIfCell>();
            for (var outcomeLeft = 0; outcomeLeft < 3; outcomeLeft++)
            {
                for (var outcomeRight = 0; outcomeRight < 3; outcomeRight++)
                {
                    if (headToHead && !MirroredOutcomes(outcomeLeft, outcomeRight))
                        continue;
                    var samples = jointCount[pairIndex, outcomeLeft, outcomeRight];
                    if (samples <= 0)
                        continue;
                    var others = otherCandidates
                        .Where(team => team.TeamId != teams[left].Id && team.TeamId != teams[right].Id)
                        .Take(3)
                        .Select(team =>
                        {
                            var otherIndex = IndexOf(teams, team.TeamId);
                            var title = otherIndex < 0 || samples <= 0
                                ? 0
                                : jointChampion[pairIndex, outcomeLeft, outcomeRight, otherIndex] / (double)samples;
                            return new JointWhatIfOther(team.TeamId, team.Name, title);
                        })
                        .ToList();
                    cells.Add(new JointWhatIfCell(
                        outcomeLeft,
                        outcomeRight,
                        jointTitleLeft[pairIndex, outcomeLeft, outcomeRight] / (double)samples,
                        jointTitleRight[pairIndex, outcomeLeft, outcomeRight] / (double)samples,
                        samples >= 200,
                        samples,
                        others));
                }
            }

            pairReports.Add(new JointWhatIfPair(teams[left].Id, teams[right].Id, headToHead, cells));
        }

        Guid defaultA;
        Guid defaultB;
        if (race?.CandidateIds.Count >= 2
            && options.Any(item => item.TeamId == race.CandidateIds[0])
            && options.Any(item => item.TeamId == race.CandidateIds[1]))
        {
            defaultA = race.CandidateIds[0];
            defaultB = race.CandidateIds[1];
        }
        else
        {
            defaultA = options[0].TeamId;
            defaultB = options[1].TeamId;
        }

        return new JointWhatIfBoard(defaultA, defaultB, options, pairReports);
    }

    private static IEnumerable<MatchOutlook> DecorateMatches(IReadOnlyList<MatchOutlook> matches, IReadOnlyList<TeamOutlook> teams)
    {
        foreach (var match in matches)
        {
            var home = teams.FirstOrDefault(team => team.TeamId == match.HomeTeamId);
            var away = teams.FirstOrDefault(team => team.TeamId == match.AwayTeamId);
            var (tone, favorite) = MatchFavoriteClassifier.Classify(
                match.HomeWin,
                match.Draw,
                match.AwayWin,
                match.HomeName,
                match.AwayName,
                home?.TitleProbability ?? 0,
                away?.TitleProbability ?? 0);
            yield return match with { FavoriteLabel = favorite, ToneLabel = tone };
        }
    }

    private static int Percentile(int[] values, double p)
    {
        var copy = (int[])values.Clone();
        Array.Sort(copy);
        var index = (int)Math.Round((copy.Length - 1) * p, MidpointRounding.AwayFromZero);
        return copy[Math.Clamp(index, 0, copy.Length - 1)];
    }

    private static int PositionPercentile(IReadOnlyList<double> distribution, double p)
    {
        if (distribution.Count == 0)
            return 0;
        double accumulated = 0;
        for (var place = 0; place < distribution.Count; place++)
        {
            accumulated += distribution[place];
            if (accumulated >= p)
                return place + 1;
        }

        return distribution.Count;
    }

    private static int Compare(List<TeamState> teams, int[] points, int[] goalsFor, int[] goalsAgainst, int a, int b)
    {
        var byPoints = points[b].CompareTo(points[a]);
        if (byPoints != 0) return byPoints;
        var byDifference = (goalsFor[b] - goalsAgainst[b]).CompareTo(goalsFor[a] - goalsAgainst[a]);
        if (byDifference != 0) return byDifference;
        var byGoals = goalsFor[b].CompareTo(goalsFor[a]);
        if (byGoals != 0) return byGoals;
        var bySeen = teams[a].Seen.CompareTo(teams[b].Seen);
        if (bySeen != 0) return bySeen;
        var byName = string.Compare(teams[a].Name, teams[b].Name, StringComparison.OrdinalIgnoreCase);
        if (byName != 0) return byName;
        return teams[a].Id.CompareTo(teams[b].Id);
    }

    private static void Apply(int[] points, int[] goalsFor, int[] goalsAgainst, int index, int scored, int conceded)
    {
        goalsFor[index] += scored;
        goalsAgainst[index] += conceded;
        if (scored > conceded) points[index] += 3;
        else if (scored == conceded) points[index] += 1;
    }

    private static int IndexOf(List<TeamState> teams, Guid id)
    {
        for (var i = 0; i < teams.Count; i++)
        {
            if (teams[i].Id == id)
                return i;
        }

        return -1;
    }

    private static double[] Pmf(double lambda, int max)
    {
        var pmf = new double[max + 1];
        var term = Math.Exp(-lambda);
        var sum = 0d;
        for (var k = 0; k <= max; k++)
        {
            pmf[k] = term;
            sum += term;
            term *= lambda / (k + 1);
            if (double.IsNaN(term) || double.IsInfinity(term))
                break;
        }

        if (sum <= 0)
        {
            pmf[0] = 1;
            return pmf;
        }

        for (var k = 0; k <= max; k++)
            pmf[k] /= sum;
        return pmf;
    }

    private static int Sample(double[] pmf, double draw)
    {
        var cumulative = 0d;
        for (var k = 0; k < pmf.Length; k++)
        {
            cumulative += pmf[k];
            if (draw <= cumulative)
                return k;
        }

        return pmf.Length - 1;
    }

    private static bool IsCountedResult(PredictionFixture fixture) =>
        fixture.Completed
        && fixture.HomeGoals is >= 0 and <= 99
        && fixture.AwayGoals is >= 0 and <= 99
        && (fixture.Home.Counts || fixture.Away.Counts);

    private static bool IsPending(PredictionFixture fixture) =>
        !fixture.Completed
        && fixture.Home.TeamId != Guid.Empty
        && fixture.Away.TeamId != Guid.Empty
        && (fixture.Home.Counts || fixture.Away.Counts);

    private static (DateTime Kickoff, int Round, Guid Id) Chronology(PredictionFixture fixture) =>
        (fixture.Kickoff ?? DateTime.MinValue, fixture.Round, fixture.Id);

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private sealed class TeamState
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OfficialPosition { get; set; }
        public int Seen { get; set; }
        public int Played { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int Points { get; set; }
        public int Remaining { get; set; }
        public bool MathematicallyCrowned { get; set; }
        public bool MathematicallyEliminated { get; set; }
        public string Form { get; set; } = "Sin partidos";
    }

    private readonly record struct Appearance(Guid OpponentId, bool OpponentKnown, int GoalsFor, int GoalsAgainst);

    private sealed class Rates
    {
        private readonly double _mu;
        private readonly Dictionary<Guid, double> _attack;
        private readonly Dictionary<Guid, double> _defense;

        public Rates(double mu, Dictionary<Guid, double> attack, Dictionary<Guid, double> defense)
        {
            _mu = mu;
            _attack = attack;
            _defense = defense;
        }

        public double Lambda(Guid attackTeam, Guid defenseTeam, bool home, PredictionOptions options)
        {
            var attack = _attack.TryGetValue(attackTeam, out var attackStrength) ? attackStrength : 1;
            var defense = _defense.TryGetValue(defenseTeam, out var defenseStrength) ? defenseStrength : 1;
            var lambda = _mu * attack * defense * (home ? options.HomeAdvantage : 1);
            return Clamp(lambda, PredictionModel.MinLambda, PredictionModel.MaxLambda);
        }
    }
}

internal static class ArgentinaClock
{
    public static DateTime Today()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time");
        }

        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).Date;
    }
}

public static class PredictionFingerprint
{
    public static string Compute(PredictionDivision division)
    {
        var builder = new StringBuilder(PredictionModel.Version);
        builder.Append('|').Append(ArgentinaClock.Today().ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        builder.Append('|').Append(division.DivisionId);
        foreach (var team in division.Baseline.OrderBy(team => team.TeamId))
        {
            builder.Append(';')
                .Append(team.TeamId).Append(':')
                .Append(team.Position).Append(':')
                .Append(team.Points).Append(':')
                .Append(team.GoalsFor).Append(':')
                .Append(team.GoalsAgainst).Append(':')
                .Append(team.Played);
        }

        foreach (var match in division.Fixtures.OrderBy(match => match.Id))
        {
            builder.Append('|')
                .Append(match.Id).Append(':')
                .Append(match.Round).Append(':')
                .Append(match.Kickoff?.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture)).Append(':')
                .Append(match.Completed ? '1' : '0').Append(':')
                .Append(match.HomeGoals).Append('-').Append(match.AwayGoals).Append(':')
                .Append(match.Home.TeamId).Append(':').Append(match.Home.Counts ? '1' : '0').Append(':')
                .Append(match.Away.TeamId).Append(':').Append(match.Away.Counts ? '1' : '0');
        }

        ulong hash = 14695981039346656037UL;
        foreach (var character in builder.ToString())
        {
            hash ^= character;
            hash *= 1099511628211UL;
        }

        return hash.ToString("x16");
    }
}

public static class PredictionBacktester
{
    public sealed record Report(
        int Matches,
        int Rounds,
        double PoissonAccuracy,
        double PointsBaselineAccuracy,
        double AlwaysDrawAccuracy,
        double PoissonBrier,
        double PointsBaselineBrier,
        double AlwaysDrawBrier,
        double PoissonLogLoss,
        double PointsBaselineLogLoss,
        double AlwaysDrawLogLoss);

    public static Report Run(IReadOnlyList<PredictionFixture> completedInOrder, PredictionOptions? options = null, int minHistory = 4)
    {
        options ??= PredictionModel.Default;
        var rounds = completedInOrder.Select(match => match.Round).Distinct().OrderBy(round => round).ToList();
        var poissonHits = 0;
        var baselineHits = 0;
        var drawHits = 0;
        double poissonBrier = 0, baselineBrier = 0, drawBrier = 0;
        double poissonLog = 0, baselineLog = 0, drawLog = 0;
        var matches = 0;
        var usedRounds = 0;

        foreach (var round in rounds)
        {
            var history = completedInOrder.Where(match => match.Completed && match.Round < round).ToList();
            var target = completedInOrder.Where(match => match.Completed && match.Round == round).ToList();
            if (history.Count < minHistory || target.Count == 0)
                continue;

            usedRounds++;
            var points = PointsSoFar(history);
            var division = new PredictionDivision(Guid.Empty, "Backtest", BaselineFrom(history), history);
            var estimates = ChampionshipPredictor.EstimateUpcoming(division, target, options);
            foreach (var match in target)
            {
                var estimate = estimates.First(item => item.MatchId == match.Id);
                var homeGoals = match.HomeGoals!.Value;
                var awayGoals = match.AwayGoals!.Value;
                var outcome = homeGoals > awayGoals ? 0 : homeGoals == awayGoals ? 1 : 2;
                if (Hit(estimate.HomeWin, estimate.Draw, estimate.AwayWin, outcome))
                    poissonHits++;
                poissonBrier += Brier(estimate.HomeWin, estimate.Draw, estimate.AwayWin, outcome);
                poissonLog += LogLoss(estimate.HomeWin, estimate.Draw, estimate.AwayWin, outcome);

                var homePoints = points.GetValueOrDefault(match.Home.TeamId);
                var awayPoints = points.GetValueOrDefault(match.Away.TeamId);
                var baseline = homePoints > awayPoints ? 0 : awayPoints > homePoints ? 2 : 1;
                if (baseline == outcome)
                    baselineHits++;
                baselineBrier += Brier(baseline == 0 ? 1 : 0, baseline == 1 ? 1 : 0, baseline == 2 ? 1 : 0, outcome);
                baselineLog += LogLoss(baseline == 0 ? 1 : 0, baseline == 1 ? 1 : 0, baseline == 2 ? 1 : 0, outcome);

                if (outcome == 1)
                    drawHits++;
                drawBrier += Brier(0, 1, 0, outcome);
                drawLog += LogLoss(0, 1, 0, outcome);
                matches++;
            }
        }

        if (matches == 0)
            return new Report(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new Report(
            matches,
            usedRounds,
            poissonHits / (double)matches,
            baselineHits / (double)matches,
            drawHits / (double)matches,
            poissonBrier / matches,
            baselineBrier / matches,
            drawBrier / matches,
            poissonLog / matches,
            baselineLog / matches,
            drawLog / matches);
    }

    private static Dictionary<Guid, int> PointsSoFar(IReadOnlyList<PredictionFixture> history)
    {
        var points = new Dictionary<Guid, int>();
        foreach (var match in history)
        {
            Add(points, match.Home, match.HomeGoals!.Value, match.AwayGoals!.Value);
            Add(points, match.Away, match.AwayGoals!.Value, match.HomeGoals!.Value);
        }

        return points;
    }

    private static void Add(Dictionary<Guid, int> points, PredictionSide side, int goalsFor, int goalsAgainst)
    {
        if (!side.Counts || side.TeamId == Guid.Empty)
            return;
        var gain = goalsFor > goalsAgainst ? 3 : goalsFor == goalsAgainst ? 1 : 0;
        points[side.TeamId] = points.GetValueOrDefault(side.TeamId) + gain;
    }

    private static List<PredictionBaseline> BaselineFrom(IReadOnlyList<PredictionFixture> history)
    {
        var map = new Dictionary<Guid, (string Name, int Played, int Gf, int Ga, int Points)>();
        foreach (var match in history)
        {
            Acc(map, match.Home, match.HomeGoals!.Value, match.AwayGoals!.Value);
            Acc(map, match.Away, match.AwayGoals!.Value, match.HomeGoals!.Value);
        }

        return map.Select(item => new PredictionBaseline(
            item.Key,
            item.Value.Name,
            0,
            item.Value.Played,
            item.Value.Gf,
            item.Value.Ga,
            item.Value.Points)).ToList();
    }

    private static void Acc(
        Dictionary<Guid, (string Name, int Played, int Gf, int Ga, int Points)> map,
        PredictionSide side,
        int goalsFor,
        int goalsAgainst)
    {
        if (!side.Counts || side.TeamId == Guid.Empty)
            return;
        map.TryGetValue(side.TeamId, out var current);
        var points = current.Points + (goalsFor > goalsAgainst ? 3 : goalsFor == goalsAgainst ? 1 : 0);
        map[side.TeamId] = (
            string.IsNullOrWhiteSpace(side.Name) ? current.Name : side.Name,
            current.Played + 1,
            current.Gf + goalsFor,
            current.Ga + goalsAgainst,
            points);
    }

    private static bool Hit(double home, double draw, double away, int outcome)
    {
        var probs = new[] { home, draw, away };
        var best = probs.Max();
        return Math.Abs(probs[outcome] - best) < 1e-9;
    }

    private static double LogLoss(double home, double draw, double away, int outcome)
    {
        var probs = new[] { home, draw, away };
        var chosen = Math.Clamp(probs[outcome], 1e-6, 1);
        return -Math.Log(chosen);
    }

    private static double Brier(double home, double draw, double away, int outcome)
    {
        var actualHome = outcome == 0 ? 1 : 0;
        var actualDraw = outcome == 1 ? 1 : 0;
        var actualAway = outcome == 2 ? 1 : 0;
        return (home - actualHome) * (home - actualHome)
            + (draw - actualDraw) * (draw - actualDraw)
            + (away - actualAway) * (away - actualAway);
    }
}
