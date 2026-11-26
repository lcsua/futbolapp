using FootballManager.Application.Predictions;
using FootballManager.Application.Standings;

namespace FootballManager.Application.Tests;

public class ChampionshipPredictorTests
{
    [Fact]
    public void Fast_rank_matches_the_official_projector()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var gamma = Guid.NewGuid();
        var division = Division(
            Row(alpha, "Alpha", 1, 10, 8, 4),
            Row(beta, "Beta", 2, 10, 7, 5),
            Row(gamma, "Gamma", 3, 7, 5, 6));
        var scores = new[]
        {
            Score(alpha, "Alpha", beta, "Beta", 1, 1),
            Score(gamma, "Gamma", alpha, "Alpha", 2, 0)
        };

        var official = ChampionshipPredictor.ProjectSample(division, scores);
        var fast = ChampionshipPredictor.RankFast(division, scores);

        Assert.Equal(official.Select(team => team.TeamId), fast.Select(team => team.TeamId));
        Assert.Equal(official.Select(team => team.Points), fast.Select(team => team.Points));
        Assert.Equal(official.Select(team => team.GoalsFor), fast.Select(team => team.GoalsFor));
        Assert.Equal(official.Select(team => team.GoalsAgainst), fast.Select(team => team.GoalsAgainst));
    }

    [Fact]
    public void Incomplete_scores_do_not_change_the_table()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var division = Division(Row(alpha, "Alpha", 1, 3, 1, 0), Row(beta, "Beta", 2, 0, 0, 1));
        var scores = new[]
        {
            new SimulatedScore(alpha, "Alpha", true, beta, "Beta", true, null, 1),
            new SimulatedScore(alpha, "Alpha", true, beta, "Beta", true, -1, 0),
            new SimulatedScore(alpha, "Alpha", true, beta, "Beta", true, 100, 0)
        };

        var ranked = ChampionshipPredictor.RankFast(division, scores);

        Assert.Equal(3, ranked.First(team => team.TeamId == alpha).Points);
        Assert.Equal(0, ranked.First(team => team.TeamId == beta).Points);
    }

    [Fact]
    public void Interzonal_only_updates_the_side_that_counts()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var division = Division(Row(away, "Visita", 1, 1, 1, 1));
        var scores = new[]
        {
            new SimulatedScore(home, "Local", false, away, "Visita", true, 0, 2)
        };

        var ranked = ChampionshipPredictor.RankFast(division, scores);
        var visit = ranked.Single(team => team.TeamId == away);

        Assert.Equal(4, visit.Points);
        Assert.Equal(3, visit.GoalsFor);
        Assert.DoesNotContain(ranked, team => team.TeamId == home);
    }

    [Fact]
    public void Sanctioned_team_can_be_eliminated_from_the_title()
    {
        var leader = Guid.NewGuid();
        var sanctioned = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                Row(leader, "Lider", 1, 12, 10, 2),
                Row(sanctioned, "Sanción", 2, 0, 4, 4)
            },
            new[]
            {
                Result(1, leader, "Lider", sanctioned, "Sanción", 1, 0),
                Result(1, leader, "Lider", sanctioned, "Sanción", 1, 0),
                Result(1, leader, "Lider", sanctioned, "Sanción", 1, 0),
                Result(1, leader, "Lider", sanctioned, "Sanción", 1, 0),
                Pending(2, leader, "Lider", sanctioned, "Sanción")
            });

        var outlook = ChampionshipPredictor.Predict(division, FewSims());
        var row = outlook.Teams.Single(team => team.TeamId == sanctioned);

        Assert.True(outlook.Published);
        Assert.Equal(0, row.CurrentPoints);
        Assert.True(row.MathematicallyEliminated);
        Assert.Equal("Eliminado", row.TitleLabel);
        Assert.Equal(12, outlook.Teams.Single(team => team.TeamId == leader).CurrentPoints);
        Assert.True(outlook.Teams.Single(team => team.TeamId == leader).MathematicallyCrowned);
        Assert.Equal("Consagrado", outlook.Teams.Single(team => team.TeamId == leader).TitleLabel);
    }

    [Fact]
    public void A_team_that_can_still_tie_is_not_called_eliminated()
    {
        var leader = Guid.NewGuid();
        var chaser = Guid.NewGuid();
        var division = Playable(leader, chaser, leaderPoints: 6, chaserPoints: 3, pendingRounds: 1);
        var outlook = ChampionshipPredictor.Predict(division, FewSims());
        var row = outlook.Teams.Single(team => team.TeamId == chaser);

        Assert.False(row.MathematicallyEliminated);
        Assert.NotEqual("Eliminado", row.TitleLabel);
        Assert.NotEqual("0%", row.TitleLabel);
        Assert.NotEqual("100%", row.TitleLabel);
    }

    [Fact]
    public void Title_probabilities_sum_to_one_and_do_not_invent_certainty()
    {
        var outlook = ChampionshipPredictor.Predict(BalancedLeague(), FewSims());

        Assert.True(outlook.Published);
        Assert.Equal(1, outlook.Teams.Sum(team => team.TitleProbability), 3);
        Assert.All(outlook.Teams, team => Assert.Equal(1, team.PositionProbability.Sum(), 3));
        Assert.All(outlook.Teams, team =>
        {
            Assert.DoesNotContain("100%", team.TitleLabel);
            Assert.DoesNotContain("0%", team.TitleLabel);
        });
        Assert.False(outlook.PromotionSupported);
        Assert.True(outlook.ChampionMeansFirstPlace);
    }

    [Fact]
    public void Too_few_matches_are_not_published()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[] { Row(home, "Local", 1, 3, 1, 0), Row(away, "Visita", 2, 0, 0, 1) },
            new[]
            {
                Result(1, home, "Local", away, "Visita", 1, 0),
                Pending(2, home, "Local", away, "Visita")
            });

        var outlook = ChampionshipPredictor.Predict(division);

        Assert.True(outlook.Supported);
        Assert.False(outlook.Published);
        Assert.Equal(1, outlook.CompletedMatches);
        Assert.Equal(1, outlook.PendingMatches);
        Assert.Contains("suficiente información", outlook.UnpublishedReason);
    }

    [Fact]
    public void Finished_championship_stays_on_the_official_table()
    {
        var home = Guid.NewGuid();
        var away = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[] { Row(home, "Local", 1, 3, 1, 0) },
            new[] { Result(1, home, "Local", away, "Visita", 1, 0) });

        var outlook = ChampionshipPredictor.Predict(division);

        Assert.True(outlook.Finished);
        Assert.False(outlook.Published);
        Assert.Equal(3, outlook.Teams.Single().CurrentPoints);
    }

    [Fact]
    public void Postponed_match_stays_pending_and_is_predicted()
    {
        var outlook = ChampionshipPredictor.Predict(BalancedLeague(postponed: true), FewSims());

        Assert.True(outlook.Published);
        Assert.True(outlook.PendingMatches > 0);
        Assert.NotEmpty(outlook.NextRound);
        Assert.All(outlook.NextRound, match =>
        {
            Assert.InRange(match.HomeWin + match.Draw + match.AwayWin, 0.99, 1.01);
            Assert.InRange(match.LikelyHomeGoals, 0, 10);
            Assert.InRange(match.LikelyAwayGoals, 0, 10);
        });
    }

    [Fact]
    public void One_blowout_does_not_produce_an_extreme_scoreline()
    {
        var strong = Guid.NewGuid();
        var weak = Guid.NewGuid();
        var other = Guid.NewGuid();
        var spare = Guid.NewGuid();
        var division = new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                Row(strong, "Fuerte", 1, 3, 8, 0),
                Row(weak, "Débil", 2, 0, 0, 8),
                Row(other, "Otro", 3, 1, 1, 1),
                Row(spare, "Resto", 4, 1, 1, 1)
            },
            new[]
            {
                Result(1, strong, "Fuerte", weak, "Débil", 8, 0),
                Result(1, other, "Otro", spare, "Resto", 1, 1),
                Result(2, strong, "Fuerte", other, "Otro", 1, 0),
                Result(2, weak, "Débil", spare, "Resto", 0, 1),
                Pending(3, strong, "Fuerte", spare, "Resto")
            });

        var outlook = ChampionshipPredictor.Predict(division, FewSims());
        var match = outlook.NextRound.Single();

        Assert.True(match.HomeLambda < 3.5);
        Assert.True(match.HomeWin > match.AwayWin);
    }

    [Fact]
    public void Unsupported_tie_break_is_not_published()
    {
        var rules = new StandingsRules(3, 1, 0, new[] { "points", "headToHead" });
        var outlook = ChampionshipPredictor.Predict(BalancedLeague(), rules: rules);

        Assert.False(outlook.Supported);
        Assert.False(outlook.Published);
        Assert.Contains("desempate", outlook.Limitation);
    }

    [Fact]
    public void Fingerprint_changes_when_a_result_changes_and_stays_stable()
    {
        var division = BalancedLeague();
        var again = PredictionFingerprint.Compute(division);
        Assert.Equal(PredictionFingerprint.Compute(division), again);

        var changed = division.Fixtures.ToList();
        var first = changed[0];
        changed[0] = first with { HomeGoals = (first.HomeGoals ?? 0) + 1 };
        var updated = division with { Fixtures = changed };

        Assert.NotEqual(again, PredictionFingerprint.Compute(updated));
    }

    [Fact]
    public void Backtest_does_not_use_future_rounds_and_beats_always_draw()
    {
        var season = SyntheticSeason();
        var early = season.Where(match => match.Round <= 2).ToList();
        var leaked = season.Where(match => match.Round <= 2 || match.Round == 7).ToList();
        var target = season.Where(match => match.Round == 3).Take(1).ToList();
        var division = new PredictionDivision(Guid.Empty, "B", Array.Empty<PredictionBaseline>(), early);
        var withFuture = new PredictionDivision(Guid.Empty, "B", Array.Empty<PredictionBaseline>(), leaked);
        var calm = ChampionshipPredictor.EstimateUpcoming(division, target).Single();
        var spoiled = ChampionshipPredictor.EstimateUpcoming(withFuture, target).Single();
        Assert.NotEqual(Math.Round(calm.HomeWin, 4), Math.Round(spoiled.HomeWin, 4));

        var report = PredictionBacktester.Run(season, FewSims(), minHistory: 4);
        var later = PredictionBacktester.Run(season, FewSims(), minHistory: 12);

        Assert.True(report.Matches > 0);
        Assert.True(report.PoissonBrier < report.AlwaysDrawBrier);
        Assert.True(report.PoissonAccuracy > report.AlwaysDrawAccuracy);
        Assert.True(later.PoissonBrier < later.PointsBaselineBrier);
    }

    [Fact]
    public void Ten_thousand_simulations_stay_within_a_couple_of_seconds()
    {
        var outlook = ChampionshipPredictor.Predict(WiderLeague(), new PredictionOptions { Simulations = 10_000, Seed = 7 });

        Assert.True(outlook.Published);
        Assert.Equal(10_000, outlook.Simulations);
        Assert.True(outlook.ElapsedMilliseconds < 2500, $"La simulación tardó {outlook.ElapsedMilliseconds} ms.");
    }

    private static PredictionOptions FewSims() => new() { Simulations = 400, Seed = 11 };

    private static PredictionDivision Division(params PredictionBaseline[] rows) =>
        new(Guid.NewGuid(), "B", rows, Array.Empty<PredictionFixture>());

    private static PredictionBaseline Row(Guid id, string name, int position, int points, int goalsFor, int goalsAgainst) =>
        new(id, name, position, position, goalsFor, goalsAgainst, points);

    private static SimulatedScore Score(Guid home, string homeName, Guid away, string awayName, int homeGoals, int awayGoals) =>
        new(home, homeName, true, away, awayName, true, homeGoals, awayGoals);

    private static PredictionFixture Result(int round, Guid home, string homeName, Guid away, string awayName, int homeGoals, int awayGoals) =>
        new(Guid.NewGuid(), round, null, new PredictionSide(home, homeName, true), new PredictionSide(away, awayName, true), homeGoals, awayGoals, true);

    private static PredictionFixture Pending(int round, Guid home, string homeName, Guid away, string awayName) =>
        new(Guid.NewGuid(), round, null, new PredictionSide(home, homeName, true), new PredictionSide(away, awayName, true), null, null, false);

    private static PredictionDivision Playable(Guid leader, Guid chaser, int leaderPoints, int chaserPoints, int pendingRounds)
    {
        var fixtures = new List<PredictionFixture>
        {
            Result(1, leader, "Lider", chaser, "Perseguidor", 1, 0),
            Result(2, leader, "Lider", chaser, "Perseguidor", 1, 0),
            Result(3, leader, "Lider", chaser, "Perseguidor", 1, 0),
            Result(4, chaser, "Perseguidor", leader, "Lider", 1, 0)
        };
        for (var round = 0; round < pendingRounds; round++)
            fixtures.Add(Pending(10 + round, leader, "Lider", chaser, "Perseguidor"));

        return new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                Row(leader, "Lider", 1, leaderPoints, 6, 2),
                Row(chaser, "Perseguidor", 2, chaserPoints, 3, 4)
            },
            fixtures);
    }

    private static PredictionDivision BalancedLeague(bool postponed = false)
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();
        var fixtures = new List<PredictionFixture>
        {
            Result(1, a, "A", b, "B", 2, 0),
            Result(1, c, "C", d, "D", 1, 1),
            Result(2, a, "A", c, "C", 1, 0),
            Result(2, b, "B", d, "D", 0, 0),
            Result(3, a, "A", d, "D", 3, 1),
            Result(3, b, "B", c, "C", 2, 2)
        };
        fixtures.Add(postponed
            ? new PredictionFixture(Guid.NewGuid(), 4, null, new PredictionSide(a, "A", true), new PredictionSide(b, "B", true), null, null, false)
            : Pending(4, a, "A", b, "B"));
        fixtures.Add(Pending(4, c, "C", d, "D"));
        fixtures.Add(Pending(5, a, "A", c, "C"));

        return new PredictionDivision(
            Guid.NewGuid(),
            "B",
            new[]
            {
                Row(a, "A", 1, 9, 6, 1),
                Row(b, "B", 2, 2, 2, 4),
                Row(c, "C", 3, 2, 3, 3),
                Row(d, "D", 4, 2, 2, 5)
            },
            fixtures);
    }

    private static PredictionDivision WiderLeague()
    {
        var ids = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        var names = Enumerable.Range(0, 10).Select(index => "Equipo " + (index + 1)).ToArray();
        var fixtures = new List<PredictionFixture>();
        var round = 1;
        for (var i = 0; i < ids.Length; i++)
        {
            for (var j = i + 1; j < ids.Length; j++)
            {
                if (fixtures.Count < 15)
                    fixtures.Add(Result(round, ids[i], names[i], ids[j], names[j], 1, 0));
                else
                    fixtures.Add(Pending(round, ids[i], names[i], ids[j], names[j]));
                if (fixtures.Count % 5 == 0)
                    round++;
            }
        }

        var baseline = ids.Select((id, index) => Row(id, names[index], index + 1, Math.Max(0, 12 - index), 8, index)).ToArray();
        return new PredictionDivision(Guid.NewGuid(), "B", baseline, fixtures);
    }

    private static List<PredictionFixture> SyntheticSeason()
    {
        var ids = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        var names = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        var strength = new[] { 2.2, 1.8, 1.5, 1.2, 1.0, 0.8, 0.6, 0.45 };
        var fixtures = new List<PredictionFixture>();
        var round = 1;
        for (var i = 0; i < ids.Length; i++)
        {
            for (var j = i + 1; j < ids.Length; j++)
            {
                var margin = strength[i] - strength[j];
                int homeGoals;
                int awayGoals;
                if (margin > 0.8)
                {
                    homeGoals = 2;
                    awayGoals = 0;
                }
                else if (margin > 0.25)
                {
                    homeGoals = 1;
                    awayGoals = 0;
                }
                else if (margin < -0.8)
                {
                    homeGoals = 0;
                    awayGoals = 2;
                }
                else if (margin < -0.25)
                {
                    homeGoals = 0;
                    awayGoals = 1;
                }
                else
                {
                    homeGoals = 1;
                    awayGoals = 1;
                }

                fixtures.Add(Result(round, ids[i], names[i], ids[j], names[j], homeGoals, awayGoals));
                if (fixtures.Count % 4 == 0)
                    round++;
            }
        }

        return fixtures;
    }
}
