using FootballManager.Api.Helpers;
using FootballManager.Api.Models.Public;
using FootballManager.Application.Predictions;
using FootballManager.Application.UseCases.Seasons.GetStandings;

namespace FootballManager.Api.Services.Public;

public sealed record PredictionSourceFixture(
    Guid Id,
    int Round,
    DateTime? Kickoff,
    string Status,
    bool HasResult,
    int? HomeGoals,
    int? AwayGoals,
    Guid FixtureDivisionSeasonId,
    Guid HomeTeamId,
    string HomeName,
    Guid HomeDivisionSeasonId,
    Guid AwayTeamId,
    string AwayName,
    Guid AwayDivisionSeasonId);

public static class PredictionComposer
{
    public static PredictionDivision Build(
        Guid divisionId,
        string divisionName,
        Guid divisionSeasonId,
        IReadOnlyList<TeamStandingDto> standings,
        IReadOnlyList<PredictionSourceFixture> fixtures)
    {
        var known = new HashSet<Guid>();
        var baseline = new List<PredictionBaseline>();
        foreach (var row in standings)
        {
            if (!known.Add(row.TeamId))
                continue;
            baseline.Add(new PredictionBaseline(
                row.TeamId,
                row.TeamName,
                row.Position,
                row.Played,
                row.GoalsFor,
                row.GoalsAgainst,
                row.Points));
        }

        var matches = new List<PredictionFixture>();
        foreach (var fixture in fixtures)
        {
            if (!Belongs(fixture, divisionSeasonId))
                continue;
            if (fixture.HomeTeamId == Guid.Empty || fixture.AwayTeamId == Guid.Empty)
                continue;
            if (IsClosedWithoutTable(fixture))
                continue;

            var completed = IsOfficialResult(fixture);
            matches.Add(new PredictionFixture(
                fixture.Id,
                fixture.Round,
                fixture.Kickoff,
                new PredictionSide(fixture.HomeTeamId, fixture.HomeName, fixture.HomeDivisionSeasonId == divisionSeasonId),
                new PredictionSide(fixture.AwayTeamId, fixture.AwayName, fixture.AwayDivisionSeasonId == divisionSeasonId),
                completed ? fixture.HomeGoals : null,
                completed ? fixture.AwayGoals : null,
                completed));
        }

        return new PredictionDivision(divisionId, divisionName, baseline, matches);
    }

    public static PredictionDivisionPublicDto ToDto(
        DivisionOutlook outlook,
        string divisionName,
        string divisionSlug,
        DateTime generatedAtUtc,
        IReadOnlyDictionary<Guid, TeamPublicDto> teams)
    {
        return new PredictionDivisionPublicDto
        {
            DivisionName = divisionName,
            DivisionSlug = divisionSlug,
            Supported = outlook.Supported,
            Limitation = outlook.Limitation,
            Published = outlook.Published,
            Finished = outlook.Finished,
            UnpublishedReason = outlook.UnpublishedReason,
            Maturity = outlook.Maturity,
            MaturityNote = outlook.MaturityNote,
            CompletedMatches = outlook.CompletedMatches,
            PendingMatches = outlook.PendingMatches,
            PendingRounds = outlook.PendingRounds,
            PlayedRatio = outlook.PlayedRatio,
            Simulations = outlook.Simulations,
            ElapsedMilliseconds = outlook.ElapsedMilliseconds,
            ModelVersion = outlook.ModelVersion,
            ChampionMeansFirstPlace = outlook.ChampionMeansFirstPlace,
            ChampionNote = outlook.ChampionNote,
            PromotionSupported = outlook.PromotionSupported,
            PromotionNote = outlook.PromotionNote,
            NextRoundNumber = outlook.NextRoundNumber,
            GeneratedAtUtc = generatedAtUtc,
            Teams = outlook.Teams.Select(team => new PredictionTeamPublicDto
            {
                Team = Team(teams, team.TeamId, team.Name),
                OfficialPosition = team.OfficialPosition,
                CurrentPoints = team.CurrentPoints,
                Played = team.Played,
                GoalsFor = team.GoalsFor,
                GoalsAgainst = team.GoalsAgainst,
                ExpectedPoints = team.ExpectedPoints,
                ExpectedPosition = team.ExpectedPosition,
                ProjectedRank = team.ProjectedRank,
                PlacesDelta = team.PlacesDelta,
                TitleProbability = team.TitleProbability,
                TitleLabel = team.TitleLabel,
                TitleKind = team.TitleKind,
                MathematicallyCrowned = team.MathematicallyCrowned,
                MathematicallyEliminated = team.MathematicallyEliminated,
                Form = team.Form,
                PositionProbability = team.PositionProbability.ToList(),
                PodiumProbability = team.PodiumProbability,
                PointsLow = team.PointsLow,
                PointsHigh = team.PointsHigh,
                PositionLow = team.PositionLow,
                PositionHigh = team.PositionHigh,
                TitleChange = TitleChange(outlook, team.TeamId)
            }).ToList(),
            NextRound = outlook.NextRound.Select(match => new PredictionMatchPublicDto
            {
                MatchId = match.MatchId,
                Round = match.Round,
                Kickoff = match.Kickoff,
                Home = Team(teams, match.HomeTeamId, match.HomeName),
                Away = Team(teams, match.AwayTeamId, match.AwayName),
                HomeWin = match.HomeWin,
                Draw = match.Draw,
                AwayWin = match.AwayWin,
                HomeWinLabel = match.HomeWinLabel,
                DrawLabel = match.DrawLabel,
                AwayWinLabel = match.AwayWinLabel,
                LikelyHomeGoals = match.LikelyHomeGoals,
                LikelyAwayGoals = match.LikelyAwayGoals,
                HomeForm = match.HomeForm,
                AwayForm = match.AwayForm,
                FavoriteLabel = match.FavoriteLabel,
                ToneLabel = match.ToneLabel
            }).ToList(),
            Race = MapRace(outlook.Race),
            Paths = outlook.Paths?.Select(MapPath).ToList() ?? new(),
            WhatIfs = outlook.WhatIfs?.Select(MapWhatIf).ToList() ?? new(),
            History = MapHistory(outlook.History),
            DecisiveMatches = outlook.DecisiveMatches?.Select(item => new PredictionDecisiveMatchPublicDto
            {
                MatchId = item.MatchId,
                Round = item.Round,
                Home = Team(teams, item.HomeTeamId, item.HomeName),
                Away = Team(teams, item.AwayTeamId, item.AwayName),
                ImpactLevel = item.ImpactLevel,
                Reason = item.Reason,
                MaxTitleSwing = item.MaxTitleSwing,
                HomeSwing = item.HomeSwing,
                AwaySwing = item.AwaySwing
            }).ToList() ?? new()
        };
    }

    public static DivisionOutlook BuildOutlook(PredictionDivision input)
    {
        var outlook = ChampionshipPredictor.Predict(input);
        if (!outlook.Published)
            return outlook;
        return outlook with { History = ChampionshipHistory.TryRebuild(input, outlook) };
    }

    private static double? TitleChange(DivisionOutlook outlook, Guid teamId)
    {
        var rounds = outlook.History?.Rounds;
        if (rounds == null || rounds.Count < 2)
            return null;
        var now = rounds[^1].Teams.FirstOrDefault(item => item.TeamId == teamId);
        var previous = rounds[^2].Teams.FirstOrDefault(item => item.TeamId == teamId);
        if (now == null || previous == null)
            return null;
        var delta = now.TitleProbability - previous.TitleProbability;
        return FootballManager.Application.Predictions.PredictionChance.IsNoChange(delta) ? null : delta;
    }

    private static PredictionRacePublicDto? MapRace(TitleRaceReport? race) =>
        race == null
            ? null
            : new PredictionRacePublicDto
            {
                Kind = race.Kind,
                Headline = race.Headline,
                Summary = race.Summary,
                PointGap = race.PointGap,
                MatchesLeft = race.MatchesLeft,
                PointsInPlay = race.PointsInPlay,
                CandidateIds = race.CandidateIds.ToList()
            };

    private static PredictionPathPublicDto MapPath(TitlePathReport path) => new()
    {
        TeamId = path.TeamId,
        Stance = path.Stance,
        StanceLabel = path.StanceLabel,
        Summary = path.Summary,
        CurrentPoints = path.CurrentPoints,
        MaxPoints = path.MaxPoints,
        RivalMax = path.RivalMax,
        PointsToClinch = path.PointsToClinch,
        PointsNote = path.PointsNote,
        BestPosition = path.BestPosition,
        WorstPosition = path.WorstPosition,
        PositionNote = path.PositionNote,
        CanClinchNextRound = path.CanClinchNextRound,
        ClinchNote = path.ClinchNote,
        Campaigns = path.Campaigns.Select(item => new PredictionCampaignPublicDto
        {
            Marks = item.Marks,
            Points = item.Points,
            SecuresTitle = item.SecuresTitle
        }).ToList()
    };

    private static PredictionWhatIfPublicDto MapWhatIf(WhatIfReport report) => new()
    {
        TeamId = report.TeamId,
        MatchId = report.MatchId,
        Round = report.Round,
        OpponentName = report.OpponentName,
        WinTitle = report.WinTitle,
        DrawTitle = report.DrawTitle,
        LossTitle = report.LossTitle,
        WinReliable = report.WinReliable,
        DrawReliable = report.DrawReliable,
        LossReliable = report.LossReliable,
        Rivals = report.Rivals.Select(rival => new PredictionWhatIfRivalPublicDto
        {
            TeamId = rival.TeamId,
            Name = rival.Name,
            IfWin = rival.IfWin,
            IfDraw = rival.IfDraw,
            IfLoss = rival.IfLoss
        }).ToList()
    };

    private static PredictionHistoryPublicDto? MapHistory(ProbabilityHistory? history) =>
        history == null
            ? null
            : new PredictionHistoryPublicDto
            {
                Available = history.Available,
                Reconstructed = history.Reconstructed,
                Note = history.Note,
                Rounds = history.Rounds.Select(round => new PredictionHistoryRoundPublicDto
                {
                    Round = round.Round,
                    Reconstructed = round.Reconstructed,
                    Teams = round.Teams.Select(team => new PredictionHistoryTeamPublicDto
                    {
                        TeamId = team.TeamId,
                        TitleProbability = team.TitleProbability,
                        PodiumProbability = team.PodiumProbability,
                        ExpectedPosition = team.ExpectedPosition,
                        ExpectedPoints = team.ExpectedPoints
                    }).ToList()
                }).ToList()
            };

    public static bool PlayedOutsideTable(PredictionSourceFixture fixture) =>
        string.Equals(fixture.Status, "PLAYED", StringComparison.OrdinalIgnoreCase) && fixture.HasResult;

    private static bool Belongs(PredictionSourceFixture fixture, Guid divisionSeasonId) =>
        fixture.FixtureDivisionSeasonId == divisionSeasonId
        || fixture.HomeDivisionSeasonId == divisionSeasonId
        || fixture.AwayDivisionSeasonId == divisionSeasonId;

    private static bool IsOfficialResult(PredictionSourceFixture fixture) =>
        string.Equals(fixture.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase)
        && fixture.HasResult
        && fixture.HomeGoals is >= 0
        && fixture.AwayGoals is >= 0;

    private static bool IsClosedWithoutTable(PredictionSourceFixture fixture)
    {
        if (string.Equals(fixture.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(fixture.Status, "PLAYED", StringComparison.OrdinalIgnoreCase) && fixture.HasResult)
            return true;
        return false;
    }

    private static TeamPublicDto Team(IReadOnlyDictionary<Guid, TeamPublicDto> teams, Guid teamId, string fallback)
    {
        if (!teams.TryGetValue(teamId, out var team))
        {
            return new TeamPublicDto
            {
                Id = teamId,
                Name = string.IsNullOrWhiteSpace(fallback) ? "Equipo" : fallback,
                Slug = SlugHelper.NormalizeSlug(fallback)
            };
        }

        return new TeamPublicDto
        {
            Id = team.Id,
            Name = string.IsNullOrWhiteSpace(team.Name) ? fallback : team.Name,
            Slug = team.Slug,
            ShortName = team.ShortName,
            LogoUrl = PublicLogo(team.LogoUrl),
            LogoThumbUrl = PublicLogo(team.LogoThumbUrl)
        };
    }

    private static string? PublicLogo(string? url) =>
        string.IsNullOrWhiteSpace(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            ? null
            : url;
}
