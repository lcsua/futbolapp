using PublicWeb.Models.Public;
using PublicWeb.Services.Public;

namespace PublicWeb.Tests;

public class MatchInsightsServiceTests
{
    private static readonly Guid HomeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AwayId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Select_BuildsThePreviewStories_FromRealSamples()
    {
        var input = new MatchInsightInput
        {
            DivisionName = "División B",
            Home = Side(
                HomeId,
                "Jueves de Sometidos",
                position: 1,
                played: 15,
                points: 34,
                wins: 11,
                draws: 1,
                losses: 3,
                goalsFor: 47,
                goalsAgainst: 16,
                recent: new[]
                {
                    R(5, 1),
                    R(4, 0),
                    R(6, 0),
                    R(4, 0),
                    R(1, 2)
                }),
            Away = Side(
                AwayId,
                "Atl Fantasmas",
                position: 9,
                played: 15,
                points: 27,
                wins: 8,
                draws: 3,
                losses: 4,
                goalsFor: 24,
                goalsAgainst: 16,
                recent: new[]
                {
                    R(1, 2),
                    R(2, 0),
                    R(3, 1),
                    R(1, 0),
                    R(1, 1)
                }),
            Division = new[]
            {
                Row(HomeId, "Jueves de Sometidos", 1, 15, 34, 47, 16),
                Row(AwayId, "Atl Fantasmas", 9, 15, 27, 24, 16),
                Row(OtherId, "Otro", 4, 15, 22, 18, 20)
            }
        };

        var insights = new MatchInsightsService().Select(input);

        Assert.Equal(4, insights.Count);
        Assert.Equal(
            new[] { "win-streak", "good-form", "best-attack", "offensive-gap" },
            insights.Select(i => i.Type).ToArray());
        Assert.Equal("Cuatro victorias consecutivas", insights[0].Title);
        Assert.Contains("convirtiendo 19 goles y recibiendo solamente uno", insights[0].Description);
        Assert.Equal(HomeId, insights[0].TeamId);
        Assert.Contains("10 de los últimos 15 puntos posibles", insights[1].Description);
        Assert.Contains("a pesar de haber perdido su último encuentro", insights[1].Description);
        Assert.Contains("47 goles en 15 partidos", insights[2].Description);
        Assert.Contains("promedia 3,13 goles por partido", insights[3].Description);
        Assert.Contains("promedia 1,60", insights[3].Description);
    }

    [Fact]
    public void Select_IsDeterministic()
    {
        var input = BothWithResults(new[] { R(1, 0), R(1, 0), R(1, 0) }, new[] { R(0, 1), R(0, 1), R(0, 1) });
        var service = new MatchInsightsService();

        var first = service.Select(input).Select(i => i.Type + i.Title).ToArray();
        var second = service.Select(input).Select(i => i.Type + i.Title).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Select_PrefersUnbeaten_WhenTheRunIsLongerThanTheWins()
    {
        var input = BothWithResults(
            new[] { R(1, 0), R(1, 0), R(1, 1), R(0, 0), R(2, 2) },
            new[] { R(0, 1) });

        var insights = new MatchInsightsService().Select(input);

        Assert.Contains(insights, i => i.Type == "unbeaten" && i.Title == "Cinco partidos sin perder");
        Assert.DoesNotContain(insights, i => i.Type == "win-streak");
    }

    [Fact]
    public void Select_EmitsLossStreak_AheadOfALongerWinlessRun()
    {
        var input = BothWithResults(
            new[] { R(0, 1), R(0, 2), R(0, 1), R(1, 1) },
            new[] { R(1, 0) });

        var insights = new MatchInsightsService().Select(input);

        Assert.Contains(insights, i => i.Type == "loss-streak");
        Assert.DoesNotContain(insights, i => i.Type == "winless");
    }

    [Fact]
    public void Select_EmitsWinless_WhenThereAreNotThreeLosses()
    {
        var input = BothWithResults(
            new[] { R(0, 1), R(1, 1), R(0, 1), R(0, 0) },
            new[] { R(1, 0) });

        var insights = new MatchInsightsService().Select(input);

        Assert.Contains(insights, i => i.Type == "winless" && i.Title == "Cuatro partidos sin ganar");
        Assert.DoesNotContain(insights, i => i.Type == "loss-streak");
    }

    [Fact]
    public void Select_EmitsCleanSheets_OnlyWhenTheyOutlastTheWinStreak()
    {
        var covered = BothWithResults(new[] { R(2, 0), R(1, 0), R(3, 0) }, new[] { R(0, 1) });
        var longer = BothWithResults(new[] { R(1, 0), R(1, 0), R(0, 0), R(0, 0) }, new[] { R(0, 1) });

        Assert.DoesNotContain(new MatchInsightsService().Select(covered), i => i.Type == "clean-sheets");
        Assert.Contains(new MatchInsightsService().Select(longer), i => i.Type == "clean-sheets");
    }

    [Fact]
    public void Select_SkipsTiedAttackAndShortSamples()
    {
        var input = new MatchInsightInput
        {
            DivisionName = "B",
            Home = Side(HomeId, "Local", 1, 4, 9, 3, 0, 1, 10, 2, new[] { R(1, 0) }),
            Away = Side(AwayId, "Visita", 2, 4, 6, 2, 0, 2, 8, 4, new[] { R(0, 1) }),
            Division = new[]
            {
                Row(HomeId, "Local", 1, 4, 9, 10, 2),
                Row(AwayId, "Visita", 2, 4, 6, 10, 4)
            }
        };

        var insights = new MatchInsightsService().Select(input);

        Assert.DoesNotContain(insights, i => i.Type is "best-attack" or "best-defense" or "high-average" or "win-streak");
    }

    [Fact]
    public void Select_SkipsBestDefense_WhenAShortSeasonHasFewerGoalsAgainst()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Corto", 3, 5, 9, 3, 0, 2, 8, 2, Array.Empty<RecentResultInput>()),
            Away = Side(AwayId, "Largo", 1, 15, 30, 9, 3, 3, 20, 4, Array.Empty<RecentResultInput>()),
            Division = new[]
            {
                Row(HomeId, "Corto", 3, 5, 9, 8, 2),
                Row(AwayId, "Largo", 1, 15, 30, 20, 4),
                Row(OtherId, "Otro", 2, 15, 28, 18, 9)
            }
        };

        var defense = new MatchInsightsService().Select(input).Single(i => i.Type == "best-defense");

        Assert.Equal(AwayId, defense.TeamId);
        Assert.Contains("4 goles en 15 partidos", defense.Description);
    }

    [Fact]
    public void Select_DescribesEqualDefenses_AndACloseTable()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Local", 4, 15, 22, 6, 4, 5, 18, 16, Array.Empty<RecentResultInput>()),
            Away = Side(AwayId, "Visita", 5, 15, 20, 5, 5, 5, 14, 16, Array.Empty<RecentResultInput>())
        };

        var insights = new MatchInsightsService().Select(input);

        var parity = Assert.Single(insights, i => i.Type == "defensive-parity");
        Assert.Contains("recibieron 16 goles en 15 encuentros", parity.Description);
        Assert.Contains("1,07", parity.Description);
        var gap = Assert.Single(insights, i => i.Type == "points-gap");
        Assert.Contains("Solo 2 puntos separan", gap.Description);
    }

    [Fact]
    public void Select_SaysTheyAreLevel_WhenPointsMatch()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Local", 2, 8, 14, 4, 2, 2, 10, 8, Array.Empty<RecentResultInput>()),
            Away = Side(AwayId, "Visita", 3, 8, 14, 4, 2, 2, 9, 9, Array.Empty<RecentResultInput>())
        };

        var gap = new MatchInsightsService().Select(input).Single(i => i.Type == "points-gap");

        Assert.Equal("Igualados en la tabla", gap.Title);
        Assert.Contains("igualados con 14 puntos", gap.Description);
    }

    [Fact]
    public void Select_DropsTopFive_WhenTheLeaderIsPlaying()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Líder", 1, 10, 24, 7, 3, 0, 15, 6, Array.Empty<RecentResultInput>()),
            Away = Side(AwayId, "Cuarto", 4, 10, 16, 5, 1, 4, 12, 11, Array.Empty<RecentResultInput>())
        };

        var insights = new MatchInsightsService().Select(input);

        Assert.Contains(insights, i => i.Type == "leader" && i.Description.Contains("sostener la primera posición"));
        Assert.DoesNotContain(insights, i => i.Type == "top-five");
    }

    [Fact]
    public void Select_AnnouncesATopFiveClash()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Segundo", 2, 10, 21, 6, 3, 1, 14, 7, Array.Empty<RecentResultInput>()),
            Away = Side(AwayId, "Quinto", 5, 10, 15, 4, 3, 3, 11, 10, Array.Empty<RecentResultInput>())
        };

        var clash = new MatchInsightsService().Select(input).Single(i => i.Type == "top-five");

        Assert.Contains("2.º", clash.Description);
        Assert.Contains("5.º", clash.Description);
    }

    [Fact]
    public void Select_ScalesRecentPoints_WhenFewerThanFiveMatchesExist()
    {
        var input = BothWithResults(
            new[] { R(1, 0), R(1, 1), R(2, 0) },
            new[] { R(1, 0), R(0, 1), R(1, 1) });

        var form = new MatchInsightsService().Select(input).Single(i => i.Type == "good-form");

        Assert.Contains("7 de los últimos 9 puntos posibles", form.Description);
    }

    [Fact]
    public void Select_DoesNotInventForm_WhenRecentSampleIsMissing()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Local", 1, 10, 20, 6, 2, 2, 12, 8, Array.Empty<RecentResultInput>(), recentKnown: false),
            Away = Side(AwayId, "Visita", 2, 10, 18, 5, 3, 2, 10, 8, Array.Empty<RecentResultInput>(), recentKnown: false)
        };

        var insights = new MatchInsightsService().Select(input);

        Assert.DoesNotContain(insights, i => i.Type is "good-form" or "win-streak" or "form-contrast");
    }

    [Fact]
    public void Select_ReturnsNothing_WhenThereIsNoUsableData()
    {
        var insights = new MatchInsightsService().Select(new MatchInsightInput
        {
            Home = new MatchSideInput { TeamId = HomeId, Name = "Local" },
            Away = new MatchSideInput { TeamId = AwayId, Name = "Visita" }
        });

        Assert.Empty(insights);
    }

    [Fact]
    public void Select_KeepsBothTeams_WhenTheCutWouldOtherwiseDropOne()
    {
        var input = new MatchInsightInput
        {
            Home = Side(HomeId, "Local", 1, 10, 22, 8, 1, 1, 20, 2, new[] { R(2, 0), R(1, 0), R(3, 1) }),
            Away = Side(AwayId, "Visita", 8, 5, 9, 3, 0, 2, 16, 20, new[] { R(2, 1), R(0, 1), R(1, 1) }),
            Division = new[]
            {
                Row(HomeId, "Local", 1, 10, 22, 20, 2),
                Row(OtherId, "Otro", 2, 10, 18, 10, 8),
                Row(AwayId, "Visita", 8, 5, 9, 16, 20)
            }
        };

        var insights = new MatchInsightsService(new MatchInsightThresholds { MaxInsights = 2 }).Select(input);

        Assert.Equal(2, insights.Count);
        Assert.Contains(insights, i => i.TeamId == HomeId);
        Assert.Contains(insights, i => i.TeamId == AwayId);
        Assert.Contains(insights, i => i.Type == "high-average");
    }

    [Fact]
    public void BarShares_NormalizesAgainstTheLargerValue_AndHandlesZeros()
    {
        Assert.Equal((1d, 0.5d), MatchPreviewBuilder.BarShares(40, 20));
        Assert.Equal((0d, 0d), MatchPreviewBuilder.BarShares(0, 0));
        Assert.Equal((0d, 0d), MatchPreviewBuilder.BarShares(null, null));
        Assert.Equal((1d, 0d), MatchPreviewBuilder.BarShares(4, null));
    }

    [Fact]
    public void Build_DoesNotTreatAMissingTeam_AsZero()
    {
        var match = Focus();
        var home = TeamSummary(HomeId, "Local", played: 6, points: 12, wins: 4, draws: 0, losses: 2, goalsFor: 9, goalsAgainst: 4);

        var preview = MatchPreviewBuilder.Build(match, standings: null, home, awayTeam: null);

        var played = preview.Rows.Single(r => r.Label == "Partidos jugados");
        Assert.Equal(6, played.Home);
        Assert.Null(played.Away);
        Assert.Equal(1d, played.HomeShare);
        Assert.False(preview.Away.HasStanding);
        Assert.Null(preview.Rows.SingleOrDefault(r => r.Label == "Puntos en los últimos 5 partidos")?.Away);
    }

    [Fact]
    public void Build_UsesRealZeros_WhenTheStandingExists()
    {
        var match = Focus();
        var home = TeamSummary(HomeId, "Local", 2, 6, 2, 0, 0, 3, 1);
        var away = TeamSummary(AwayId, "Visita", 2, 0, 0, 0, 2, 1, 4);

        var preview = MatchPreviewBuilder.Build(match, null, home, away);
        var draws = preview.Rows.Single(r => r.Label == "Empates");

        Assert.Equal(0, draws.Home);
        Assert.Equal(0, draws.Away);
        Assert.Equal((0d, 0d), (draws.HomeShare, draws.AwayShare));
    }

    [Fact]
    public void Build_IgnoresTheMatchItself_LaterResults_AndSuspensions()
    {
        var match = Focus();
        match.Kickoff = new DateTime(2026, 11, 28, 16, 0, 0);
        var home = TeamSummary(HomeId, "Local", 4, 6, 2, 0, 2, 5, 4);
        home.LastResults = new List<MatchViewModel>
        {
            Result(Guid.NewGuid(), HomeId, AwayId, 4, 0, new DateTime(2026, 12, 5), "COMPLETED"),
            Result(match.Id, HomeId, AwayId, 1, 0, match.Kickoff, "COMPLETED"),
            Result(Guid.NewGuid(), HomeId, AwayId, 0, 1, new DateTime(2026, 11, 20), "SUSPENDED"),
            Result(Guid.NewGuid(), AwayId, HomeId, 0, 2, new DateTime(2026, 11, 13), "COMPLETED"),
            Result(Guid.NewGuid(), HomeId, AwayId, 1, 1, new DateTime(2026, 11, 6), "COMPLETED")
        };

        var preview = MatchPreviewBuilder.Card(match, home, awayTeam: null);

        Assert.Equal(new[] { 'G', 'E' }, preview.Home.Form);
    }

    [Fact]
    public void Build_AdjustsLastFivePoints_ToTheMatchesActuallyPlayed()
    {
        var match = Focus();
        var home = TeamSummary(HomeId, "Local", 3, 7, 2, 1, 0, 4, 1);
        home.LastResults = new List<MatchViewModel>
        {
            Result(Guid.NewGuid(), HomeId, AwayId, 2, 0, new DateTime(2026, 11, 1), "COMPLETED"),
            Result(Guid.NewGuid(), HomeId, AwayId, 1, 1, new DateTime(2026, 10, 25), "COMPLETED"),
            Result(Guid.NewGuid(), HomeId, AwayId, 1, 0, new DateTime(2026, 10, 18), "COMPLETED")
        };

        var preview = MatchPreviewBuilder.Build(match, null, home, awayTeam: null);
        var recent = preview.Rows.Single(r => r.Label == "Puntos en los últimos 5 partidos");

        Assert.Equal(7, recent.Home);
        Assert.Null(recent.Away);
    }

    [Fact]
    public void Build_HidesComparison_WhenNeitherSideHasData()
    {
        var preview = MatchPreviewBuilder.Build(Focus(), null, null, null);

        Assert.False(preview.HasComparison);
        Assert.Empty(preview.Rows);
        Assert.Empty(preview.Insights);
    }

    [Fact]
    public void IsScheduled_OnlyMatchesTheOpenState()
    {
        Assert.True(PublicWeb.Helpers.TeamDisplayHelper.IsScheduled("SCHEDULED"));
        Assert.True(PublicWeb.Helpers.TeamDisplayHelper.IsScheduled(" scheduled "));
        Assert.False(PublicWeb.Helpers.TeamDisplayHelper.IsScheduled("COMPLETED"));
        Assert.False(PublicWeb.Helpers.TeamDisplayHelper.IsScheduled("IN_PROGRESS"));
        Assert.False(PublicWeb.Helpers.TeamDisplayHelper.IsScheduled("SUSPENDED"));
    }

    private static MatchInsightInput BothWithResults(RecentResultInput[] homeRecent, RecentResultInput[] awayRecent) =>
        new()
        {
            Home = Side(HomeId, "Local", 3, 8, 12, 3, 3, 2, 10, 8, homeRecent),
            Away = Side(AwayId, "Visita", 6, 8, 9, 2, 3, 3, 8, 11, awayRecent)
        };

    private static MatchSideInput Side(
        Guid id,
        string name,
        int position,
        int played,
        int points,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst,
        IReadOnlyList<RecentResultInput> recent,
        bool recentKnown = true) => new()
    {
        TeamId = id,
        Name = name,
        HasStanding = true,
        Position = position,
        Played = played,
        Points = points,
        Wins = wins,
        Draws = draws,
        Losses = losses,
        GoalsFor = goalsFor,
        GoalsAgainst = goalsAgainst,
        RecentKnown = recentKnown,
        Recent = recent
    };

    private static DivisionStandingInput Row(Guid id, string name, int position, int played, int points, int goalsFor, int goalsAgainst) =>
        new()
        {
            TeamId = id,
            Name = name,
            Position = position,
            Played = played,
            Points = points,
            GoalsFor = goalsFor,
            GoalsAgainst = goalsAgainst
        };

    private static RecentResultInput R(int goalsFor, int goalsAgainst) =>
        new() { GoalsFor = goalsFor, GoalsAgainst = goalsAgainst };

    private static MatchViewModel Focus() => new()
    {
        Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
        Status = "SCHEDULED",
        Kickoff = new DateTime(2026, 11, 28),
        DivisionName = "División B",
        HomeTeam = new TeamViewModel { Id = HomeId, Name = "Local", Slug = "local" },
        AwayTeam = new TeamViewModel { Id = AwayId, Name = "Visita", Slug = "visita" }
    };

    private static TeamDetailViewModel TeamSummary(
        Guid id,
        string name,
        int played,
        int points,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst) => new()
    {
        Team = new TeamViewModel { Id = id, Name = name, Slug = name.ToLowerInvariant() },
        Standing = new StandingSummaryViewModel
        {
            Position = 1,
            Played = played,
            Points = points,
            Wins = wins,
            Draws = draws,
            Losses = losses,
            GoalsFor = goalsFor,
            GoalsAgainst = goalsAgainst
        }
    };

    private static MatchViewModel Result(Guid id, Guid homeId, Guid awayId, int homeScore, int awayScore, DateTime kickoff, string status) =>
        new()
        {
            Id = id,
            Status = status,
            Kickoff = kickoff,
            HomeScore = homeScore,
            AwayScore = awayScore,
            HomeTeam = new TeamViewModel { Id = homeId, Name = "Local" },
            AwayTeam = new TeamViewModel { Id = awayId, Name = "Visita" }
        };
}
