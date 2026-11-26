using PublicWeb.Models.Public;
using PublicWeb.Services.Public;

namespace PublicWeb.Tests;

public class TeamDashboardBuilderTests
{
    private static readonly Guid TeamId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid RivalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Numbers_UseTheStandingRow_IncludingPointsThatDoNotMatchWins()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(
            standing: Row(played: 15, wins: 11, draws: 2, losses: 2, goalsFor: 47, goalsAgainst: 16, points: 30)));

        var numbers = dashboard.Numbers!;
        Assert.Equal(73, numbers.WinPercent);
        Assert.Equal(67, numbers.PointsPercent);
        Assert.Equal("3,1", numbers.GoalsForAverage);
        Assert.Equal("1,1", numbers.GoalsAgainstAverage);
        Assert.Equal(11, numbers.Wins);
        Assert.Equal(2, numbers.Draws);
        Assert.Equal(2, numbers.Losses);
        Assert.Equal(47, numbers.GoalsFor);
        Assert.Equal(16, numbers.GoalsAgainst);
    }

    [Fact]
    public void Numbers_StayEmpty_WhenTheTeamHasNotPlayed()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(standing: Row(played: 0, wins: 0, draws: 0, losses: 0, goalsFor: 0, goalsAgainst: 0, points: 0)));

        Assert.NotNull(dashboard.Numbers);
        Assert.Equal(0, dashboard.Numbers!.Played);
        Assert.Null(dashboard.Numbers.WinPercent);
        Assert.Null(dashboard.Evolution);
        Assert.Empty(dashboard.Insights);
    }

    [Fact]
    public void Evolution_ReadsGoalsFromTheTeamSide_AndKeepsChronologicalOrder()
    {
        var matches = new List<MatchViewModel>
        {
            Result(3, new DateTime(2026, 11, 20), homeGoals: 0, awayGoals: 1, teamIsHome: false, round: 16),
            Result(2, new DateTime(2026, 11, 10), homeGoals: 2, awayGoals: 0, teamIsHome: true, round: 15),
            Result(1, new DateTime(2026, 11, 1), homeGoals: 1, awayGoals: 1, teamIsHome: true, round: 14, status: "SUSPENDED")
        };

        var dashboard = TeamDashboardBuilder.Build(Team(results: matches));
        var evolution = dashboard.Evolution!;

        Assert.Equal(new[] { "F15", "F16" }, evolution.Matches.Select(m => m.Label).ToArray());
        Assert.Equal(2, evolution.Matches[0].GoalsFor);
        Assert.Equal(0, evolution.Matches[0].GoalsAgainst);
        Assert.Equal(1, evolution.Matches[1].GoalsFor);
        Assert.Equal(0, evolution.Matches[1].GoalsAgainst);
        Assert.Contains("Victoria 1 — 0", evolution.Matches[1].Detail, StringComparison.Ordinal);
        Assert.Contains("contra Rival", evolution.Matches[1].Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Evolution_ShowsOnlyTheLoadedFinishedMatches_UpToTen()
    {
        var results = Enumerable.Range(1, 12)
            .Select(day => Result(day, new DateTime(2026, 1, day), 1, 0, teamIsHome: true, round: day))
            .Reverse()
            .ToList();

        var few = TeamDashboardBuilder.Build(Team(results: results.Take(4).ToList()));
        Assert.Equal(4, few.Evolution!.Matches.Count);
        Assert.Equal("Últimos 4 partidos", few.Evolution.Title);
        Assert.Equal("F9", few.Evolution.Matches[0].Label);

        var many = TeamDashboardBuilder.Build(Team(results: results));
        Assert.Equal(10, many.Evolution!.Matches.Count);
        Assert.Equal("Últimos 10 partidos", many.Evolution.Title);
        Assert.Equal("F3", many.Evolution.Matches[0].Label);
        Assert.Equal("F12", many.Evolution.Matches[^1].Label);
    }

    [Fact]
    public void Insights_ReuseTheMatchEngine_ForAWinningAndALosingRun()
    {
        var wins = Enumerable.Range(0, 4)
            .Select(i => Result(i, new DateTime(2026, 11, 20).AddDays(-i), 2, 0, teamIsHome: true, round: 10 - i))
            .ToList();
        var winning = TeamDashboardBuilder.Build(Team(
            standing: Row(4, 4, 0, 0, 8, 0, 12),
            results: wins));
        Assert.Contains(winning.Insights, i => i.Type == "win-streak");
        Assert.InRange(winning.Insights.Count, 1, 3);

        var losses = Enumerable.Range(0, 4)
            .Select(i => Result(i, new DateTime(2026, 11, 20).AddDays(-i), 0, 2, teamIsHome: true, round: 10 - i))
            .ToList();
        var losing = TeamDashboardBuilder.Build(Team(
            standing: Row(4, 0, 0, 4, 0, 8, 0),
            results: losses));
        Assert.Contains(losing.Insights, i => i.Type == "loss-streak");
    }

    [Fact]
    public void Scorers_KeepEveryoneTiedForTheThirdPlace_AndDropAPublicPhotoThatIsNotAnImageAddress()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(scorers: new List<TeamScorerViewModel>
        {
            new() { PlayerName = "Picha", Goals = 7, PhotoUrl = "data:image/png;base64,aaaa" },
            new() { PlayerName = "Bachicha", Goals = 8, PhotoUrl = "https://miliga.com.ar/foto.jpg" },
            new() { PlayerName = "Pony", Goals = 7 },
            new() { PlayerName = "Lucho", Goals = 7 },
            new() { PlayerName = "Nico", Goals = 5 }
        }));

        Assert.Equal(new[] { "Bachicha", "Lucho", "Picha", "Pony" }, dashboard.Scorers.Select(s => s.Name).ToArray());
        Assert.Equal(new[] { 1, 2, 2, 2 }, dashboard.Scorers.Select(s => s.Rank).ToArray());
        Assert.Equal("https://miliga.com.ar/foto.jpg", dashboard.Scorers[0].PhotoUrl);
        Assert.Null(dashboard.Scorers.Single(s => s.Name == "Picha").PhotoUrl);
        Assert.True(dashboard.HasMoreScorers);
        Assert.Equal("8 goles", dashboard.Scorers[0].GoalsLabel);
    }

    [Fact]
    public void Road_CountsThreePointsPerRemainingMatch_FromThePublishedTotal()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(
            standing: Row(15, 11, 2, 2, 47, 16, 35),
            nextTotal: 4,
            next: new List<MatchViewModel>
            {
                Upcoming(17, teamIsHome: false),
                Upcoming(18, teamIsHome: true),
                Upcoming(19, teamIsHome: true),
                Upcoming(20, teamIsHome: true)
            }));

        var road = dashboard.Road!;
        Assert.False(road.Finished);
        Assert.Equal("4 partidos", road.Headline);
        Assert.Equal("12 puntos en juego", road.Detail);
        Assert.Equal("Máximo alcanzable: 47 pts", road.Extra);
        Assert.Equal(3, road.Rivals.Count);
        Assert.True(road.HasMoreMatches);
        Assert.Equal("Rival", road.Rivals[0].Team.Name);
        Assert.Equal(RivalId, road.Rivals[0].Match.HomeTeam.Id);
    }

    [Fact]
    public void Road_BecomesTheFinalRecord_WhenTheSeasonIsClosed()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(
            standing: Row(15, 11, 2, 2, 47, 16, 35, division: "División B", position: 1),
            seasonActive: false,
            nextTotal: 2,
            next: new List<MatchViewModel> { Upcoming(17, teamIsHome: true) }));

        var road = dashboard.Road!;
        Assert.True(road.Finished);
        Assert.Equal("1.º · 35 pts", road.Headline);
        Assert.Contains("División B", road.Detail, StringComparison.Ordinal);
        Assert.Contains("+31 de diferencia", road.Detail, StringComparison.Ordinal);
        Assert.Contains("11 victorias", road.Extra, StringComparison.Ordinal);
        Assert.Empty(road.Rivals);
        Assert.False(road.HasMoreMatches);
    }

    [Fact]
    public void Road_HidesTheMoreMatchesLink_WhenEveryRemainingFixtureIsListed()
    {
        var dashboard = TeamDashboardBuilder.Build(Team(
            standing: Row(10, 4, 3, 3, 12, 10, 15),
            nextTotal: 2,
            next: new List<MatchViewModel>
            {
                Upcoming(17, teamIsHome: true),
                Upcoming(18, teamIsHome: false)
            }));

        Assert.Equal(2, dashboard.Road!.Rivals.Count);
        Assert.False(dashboard.Road.HasMoreMatches);
    }

    [Fact]
    public void Road_SaysNothingIsLeft_WithoutInventingAMaximumWhenThereIsNoTable()
    {
        var open = TeamDashboardBuilder.Build(Team());
        Assert.Null(open.Road);

        var done = TeamDashboardBuilder.Build(Team(nextTotal: 0, standing: Row(10, 4, 3, 3, 12, 10, 15)));
        Assert.Equal("No quedan partidos", done.Road!.Headline);
        Assert.Equal("Lleva 15 puntos.", done.Road.Extra);
        Assert.Empty(done.Road.Rivals);
    }

    private static TeamDetailViewModel Team(
        StandingSummaryViewModel? standing = null,
        List<MatchViewModel>? results = null,
        List<MatchViewModel>? next = null,
        int nextTotal = 0,
        List<TeamScorerViewModel>? scorers = null,
        bool seasonActive = true)
    {
        return new TeamDetailViewModel
        {
            Team = new TeamViewModel { Id = TeamId, Name = "Jueves de Sometidos" },
            Season = new SeasonViewModel { Name = "Clausura 2026", Slug = "clausura-2026", IsActive = seasonActive },
            Standing = standing,
            LastResults = results ?? new List<MatchViewModel>(),
            NextMatches = next ?? new List<MatchViewModel>(),
            NextMatchesTotal = nextTotal,
            Scorers = scorers ?? new List<TeamScorerViewModel>()
        };
    }

    private static StandingSummaryViewModel Row(
        int played,
        int wins,
        int draws,
        int losses,
        int goalsFor,
        int goalsAgainst,
        int points,
        string? division = "División B",
        int position = 1) => new()
    {
        Position = position,
        Played = played,
        Wins = wins,
        Draws = draws,
        Losses = losses,
        GoalsFor = goalsFor,
        GoalsAgainst = goalsAgainst,
        Points = points,
        DivisionName = division
    };

    private static MatchViewModel Result(
        int id,
        DateTime kickoff,
        int homeGoals,
        int awayGoals,
        bool teamIsHome,
        int round,
        string status = "COMPLETED")
    {
        var team = new TeamViewModel { Id = TeamId, Name = "Jueves de Sometidos" };
        var rival = new TeamViewModel { Id = RivalId, Name = "Rival" };
        return new MatchViewModel
        {
            Id = Guid.NewGuid(),
            Status = status,
            Kickoff = kickoff,
            RoundNumber = round,
            SeasonSlug = "clausura-2026",
            HomeTeam = teamIsHome ? team : rival,
            AwayTeam = teamIsHome ? rival : team,
            HomeScore = homeGoals,
            AwayScore = awayGoals,
            LeagueSlug = "veteranos-de-perico"
        };
    }

    private static MatchViewModel Upcoming(int round, bool teamIsHome)
    {
        var team = new TeamViewModel { Id = TeamId, Name = "Jueves de Sometidos", Slug = "jueves-de-sometidos" };
        var rival = new TeamViewModel { Id = RivalId, Name = "Rival", Slug = "rival" };
        return new MatchViewModel
        {
            Id = Guid.NewGuid(),
            Status = "SCHEDULED",
            Kickoff = new DateTime(2026, 12, round),
            RoundNumber = round,
            SeasonSlug = "clausura-2026",
            HomeTeam = teamIsHome ? team : rival,
            AwayTeam = teamIsHome ? rival : team,
            LeagueSlug = "veteranos-de-perico"
        };
    }
}
