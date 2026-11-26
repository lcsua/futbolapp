using FootballManager.Application.Predictions;

namespace FootballManager.Application.Tests;

public class PredictionStoryTests
{
    [Fact]
    public void A_team_depends_on_itself_only_when_winning_out_is_strictly_enough()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var teams = new[]
        {
            Team(alpha, "Alpha", 10, 0.7),
            Team(beta, "Beta", 7, 0.3)
        };
        var pending = new[]
        {
            Match(2, alpha, "Alpha", Guid.NewGuid(), "Otro"),
            Match(2, beta, "Beta", Guid.NewGuid(), "Ajeno")
        };

        var paths = TitlePathCalculator.Build(teams, pending);
        var leader = paths.Single(path => path.TeamId == alpha);
        var chaser = paths.Single(path => path.TeamId == beta);

        Assert.Equal("self", leader.Stance);
        Assert.True(leader.CanClinchNextRound);
        Assert.Equal("help", chaser.Stance);
        Assert.False(chaser.CanClinchNextRound);
    }

    [Fact]
    public void Equal_points_are_not_a_mathematical_clinch()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var teams = new[]
        {
            Team(alpha, "Alpha", 9, 0.5),
            Team(beta, "Beta", 9, 0.5)
        };
        var pending = new[]
        {
            Match(2, alpha, "Alpha", Guid.NewGuid(), "Otro"),
            Match(2, beta, "Beta", Guid.NewGuid(), "Ajeno")
        };

        var leader = TitlePathCalculator.Build(teams, pending).Single(path => path.TeamId == alpha);

        Assert.Equal("help", leader.Stance);
        Assert.Contains("igualarlo", leader.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_team_that_cannot_catch_the_leader_is_eliminated()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var teams = new[]
        {
            Team(alpha, "Alpha", 12, 0.99),
            Team(beta, "Beta", 3, 0.01, eliminated: true)
        };

        var chaser = TitlePathCalculator.Build(teams, Array.Empty<PredictionFixture>()).Single(path => path.TeamId == beta);

        Assert.Equal("eliminated", chaser.Stance);
        Assert.Equal(2, chaser.BestPosition);
        Assert.True(chaser.WorstPosition >= 2);
    }

    [Fact]
    public void History_refuses_to_invent_a_series_without_kickoffs()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var fixtures = new List<PredictionFixture>();
        for (var round = 1; round <= 4; round++)
            fixtures.Add(Played(round, null, alpha, beta, 1, 0));
        fixtures.Add(Match(5, alpha, "Alpha", beta, "Beta"));
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 4, 4, 0, 12),
                new PredictionBaseline(beta, "Beta", 2, 4, 0, 4, 0)
            },
            fixtures);
        var live = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 200, MinCompletedMatches = 4 }, includeStory: false);

        var history = ChampionshipHistory.TryRebuild(division, live, new PredictionOptions { Simulations = 50, MinCompletedMatches = 4 });

        Assert.False(history.Available);
        Assert.Contains("fechas", history.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void History_marks_earlier_rounds_as_reconstructed()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var gamma = Guid.NewGuid();
        var fixtures = new List<PredictionFixture>
        {
            Played(1, new DateTime(2026, 8, 1), alpha, beta, 2, 0),
            Played(1, new DateTime(2026, 8, 1), gamma, Guid.NewGuid(), 1, 1),
            Played(2, new DateTime(2026, 8, 8), alpha, gamma, 1, 0),
            Played(2, new DateTime(2026, 8, 8), beta, Guid.NewGuid(), 0, 0),
            Played(3, new DateTime(2026, 8, 15), beta, alpha, 0, 1),
            Played(3, new DateTime(2026, 8, 15), gamma, Guid.NewGuid(), 2, 1),
            Match(4, alpha, "Alpha", beta, "Beta")
        };
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 3, 4, 0, 9),
                new PredictionBaseline(beta, "Beta", 2, 3, 0, 3, 1),
                new PredictionBaseline(gamma, "Gamma", 3, 2, 3, 2, 4)
            },
            fixtures);
        var live = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 300, MinCompletedMatches = 4 });

        var history = ChampionshipHistory.TryRebuild(division, live, new PredictionOptions { Simulations = 80, MinCompletedMatches = 4 });

        Assert.True(history.Available);
        Assert.True(history.Reconstructed);
        Assert.Contains(history.Rounds, round => round.Reconstructed);
        Assert.False(history.Rounds[^1].Reconstructed);
        Assert.Equal(live.Teams.Single(team => team.TeamId == alpha).TitleProbability, history.Rounds[^1].Teams.Single(team => team.TeamId == alpha).TitleProbability);
    }

    [Fact]
    public void History_refuses_when_any_played_match_has_no_kickoff()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var fixtures = new List<PredictionFixture>();
        for (var round = 1; round <= 9; round++)
            fixtures.Add(Played(round, new DateTime(2026, 8, round), alpha, beta, 1, 0));
        fixtures.Add(Played(10, null, alpha, beta, 2, 0));
        fixtures.Add(Match(11, alpha, "Alpha", beta, "Beta"));
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 10, 11, 0, 30),
                new PredictionBaseline(beta, "Beta", 2, 10, 0, 11, 0)
            },
            fixtures);
        var live = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 40, MinCompletedMatches = 4 }, includeStory: false);

        var history = ChampionshipHistory.TryRebuild(division, live, new PredictionOptions { Simulations = 40, MinCompletedMatches = 4 });

        Assert.False(history.Available);
        Assert.Contains("fechas", history.Note, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void History_does_not_label_a_late_earlier_round_after_a_later_one()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var fixtures = new List<PredictionFixture>
        {
            Played(1, new DateTime(2026, 8, 1), alpha, beta, 1, 0),
            Played(2, new DateTime(2026, 8, 8), alpha, beta, 1, 0),
            Played(3, new DateTime(2026, 8, 15), alpha, beta, 1, 0),
            Played(4, new DateTime(2026, 8, 22), alpha, beta, 1, 0),
            Played(6, new DateTime(2026, 9, 5), alpha, beta, 1, 0),
            Played(5, new DateTime(2026, 9, 12), alpha, beta, 1, 0),
            Match(7, alpha, "Alpha", beta, "Beta")
        };
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 6, 6, 0, 18),
                new PredictionBaseline(beta, "Beta", 2, 6, 0, 6, 0)
            },
            fixtures);
        var live = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 40, MinCompletedMatches = 4 }, includeStory: false);

        var history = ChampionshipHistory.TryRebuild(division, live, new PredictionOptions { Simulations = 40, MinCompletedMatches = 4 });

        Assert.True(history.Available);
        Assert.DoesNotContain(history.Rounds, round => round.Round == 5);
        for (var index = 1; index < history.Rounds.Count; index++)
            Assert.True(history.Rounds[index].Round >= history.Rounds[index - 1].Round);
    }

    [Fact]
    public void A_rival_match_keeps_the_best_position_from_being_called_secure()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var gamma = Guid.NewGuid();
        var teams = new[]
        {
            Team(alpha, "Alpha", 10, 0.4),
            Team(beta, "Beta", 12, 0.3),
            Team(gamma, "Gamma", 12, 0.3)
        };
        var pending = new[]
        {
            Match(2, alpha, "Alpha", Guid.NewGuid(), "Otro"),
            Match(2, beta, "Beta", gamma, "Gamma")
        };

        var leader = TitlePathCalculator.Build(teams, pending).Single(path => path.TeamId == alpha);

        Assert.Contains("no es un puesto asegurado", leader.PositionNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void What_if_uses_the_dated_upcoming_match_ahead_of_an_undated_one()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 4, 8, 2, 10),
                new PredictionBaseline(beta, "Beta", 2, 4, 4, 4, 6)
            },
            new[]
            {
                Played(1, new DateTime(2026, 8, 1), alpha, beta, 2, 0),
                Played(2, new DateTime(2026, 8, 8), alpha, Guid.NewGuid(), 1, 0),
                Played(3, new DateTime(2026, 8, 15), beta, Guid.NewGuid(), 1, 1),
                Played(4, new DateTime(2026, 8, 22), alpha, Guid.NewGuid(), 3, 1),
                new PredictionFixture(Guid.NewGuid(), 12, null, new PredictionSide(alpha, "Alpha", true), new PredictionSide(beta, "Beta", true), null, null, false),
                Match(8, alpha, "Alpha", beta, "Beta")
            });

        var outlook = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 200, MinCompletedMatches = 4 });

        Assert.Equal(8, outlook.WhatIfs!.Single(item => item.TeamId == alpha).Round);
    }

    [Fact]
    public void What_if_probabilities_stay_between_zero_and_one()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                new PredictionBaseline(alpha, "Alpha", 1, 4, 8, 2, 10),
                new PredictionBaseline(beta, "Beta", 2, 4, 4, 4, 6)
            },
            new[]
            {
                Played(1, new DateTime(2026, 8, 1), alpha, beta, 2, 0),
                Played(2, new DateTime(2026, 8, 8), alpha, Guid.NewGuid(), 1, 0),
                Played(3, new DateTime(2026, 8, 15), beta, Guid.NewGuid(), 1, 1),
                Played(4, new DateTime(2026, 8, 22), alpha, Guid.NewGuid(), 3, 1),
                Match(5, alpha, "Alpha", beta, "Beta")
            });

        var outlook = ChampionshipPredictor.Predict(division, new PredictionOptions { Simulations = 400, MinCompletedMatches = 4 });
        var report = outlook.WhatIfs!.Single(item => item.TeamId == alpha);

        Assert.InRange(report.WinTitle, 0, 1);
        Assert.InRange(report.DrawTitle, 0, 1);
        Assert.InRange(report.LossTitle, 0, 1);
        Assert.True(report.WinTitle >= report.LossTitle);
        Assert.All(outlook.Teams, team => Assert.True(team.PointsLow <= team.PointsHigh));
        Assert.All(outlook.Teams, team => Assert.True(team.PositionLow <= team.PositionHigh));
        Assert.All(outlook.NextRound, match => Assert.False(string.IsNullOrWhiteSpace(match.ToneLabel)));
    }

    private static TeamOutlook Team(Guid id, string name, int points, double title, bool eliminated = false) =>
        new(id, name, 1, points, 4, points, 2, points, 1, 1, 0, title, "", "estimate", false, eliminated, "", Array.Empty<double>());

    private static PredictionFixture Match(int round, Guid home, string homeName, Guid away, string awayName) =>
        new(Guid.NewGuid(), round, new DateTime(2026, 11, 28), new PredictionSide(home, homeName, true), new PredictionSide(away, awayName, true), null, null, false);

    private static PredictionFixture Played(int round, DateTime? kickoff, Guid home, Guid away, int homeGoals, int awayGoals) =>
        new(Guid.NewGuid(), round, kickoff, new PredictionSide(home, "Alpha", true), new PredictionSide(away, away == Guid.Empty ? "Ajeno" : "Beta", true), homeGoals, awayGoals, true);
}
