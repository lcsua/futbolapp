using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Tests;

public class LeagueHomeComposerTests
{
    [Fact]
    public void Compose_BuildsDivisionPanelsFromExistingStandingsAndCalendar()
    {
        var home = LeagueHomeComposer.Compose(
            new LeagueViewModel { Name = "Liga Test", Slug = "liga-test" },
            "Clausura 2026",
            "clausura-2026",
            new[]
            {
                new DivisionViewModel { Name = "A", Slug = "a" },
                new DivisionViewModel { Name = "Senior", Slug = "senior" }
            },
            Standings(),
            Calendar(),
            "senior");

        Assert.Equal("senior", home.SelectedDivisionSlug);
        Assert.Equal(2, home.DivisionPanels.Count);
        Assert.Equal(2, home.Stats.DivisionCount);
        Assert.Equal(4, home.Stats.TeamCount);
        Assert.Equal(2, home.Stats.CurrentRound);
        Assert.Equal("En curso", home.Stats.CurrentRoundStatus);

        var senior = Assert.Single(home.DivisionPanels, p => p.DivisionSlug == "senior");
        Assert.Equal(2, senior.Round);
        Assert.Equal(3, senior.MatchCount);
        Assert.Equal(3, senior.Matches.Count);
        Assert.Equal(LeagueHomeComposer.MatchPreviewCount, senior.Matches.Count);
        Assert.Equal(2, senior.StandingsPreview.Count);
        Assert.Equal("Los Amigos", senior.StandingsPreview[0].Team.Name);
        Assert.Equal(2, senior.Teams.Count);
    }

    [Fact]
    public void Compose_LimitsMatchPreview_AndKeepsTotalCount()
    {
        var matches = Enumerable.Range(1, 8).Select(i => Match($"Local {i}", $"Visitante {i}", "Scheduled")).ToList();
        var calendar = new SeasonGroupedViewModel<MatchdayGroupViewModel>
        {
            Divisions =
            {
                new DivisionGroupViewModel<MatchdayGroupViewModel>
                {
                    DivisionName = "A",
                    DivisionSlug = "a",
                    DefaultRound = 1,
                    Data = { new MatchdayGroupViewModel { Round = 1, Matches = matches } }
                }
            }
        };

        var home = LeagueHomeComposer.Compose(
            new LeagueViewModel { Name = "Liga", Slug = "liga" },
            "Apertura",
            "apertura",
            new[] { new DivisionViewModel { Name = "A", Slug = "a" } },
            null,
            calendar,
            null);

        var panel = Assert.Single(home.DivisionPanels);
        Assert.Equal(8, panel.MatchCount);
        Assert.Equal(3, panel.Matches.Count);
        Assert.Null(home.Stats.TeamCount);
        Assert.Equal(1, home.Stats.DivisionCount);
    }

    [Fact]
    public void Compose_EmptyCalendarAndStandings_StillRendersDivisionChips()
    {
        var home = LeagueHomeComposer.Compose(
            new LeagueViewModel { Name = "Liga", Slug = "liga" },
            "Apertura",
            "apertura",
            new[] { new DivisionViewModel { Name = "A", Slug = "a" } },
            null,
            null,
            null);

        var panel = Assert.Single(home.DivisionPanels);
        Assert.Empty(panel.Matches);
        Assert.Empty(panel.StandingsPreview);
        Assert.Empty(panel.Teams);
        Assert.Equal("a", home.SelectedDivisionSlug);
        Assert.Null(home.Stats.CurrentRound);
        Assert.Null(home.Stats.TeamCount);
    }

    [Fact]
    public void Compose_OrdersHomeTeamsWithRealCrestsFirst()
    {
        var standings = new SeasonGroupedViewModel<StandingsRowViewModel>
        {
            Divisions =
            {
                new DivisionGroupViewModel<StandingsRowViewModel>
                {
                    DivisionName = "A",
                    DivisionSlug = "a",
                    Data =
                    {
                        Row(1, "Sin Escudo", 9),
                        new StandingsRowViewModel
                        {
                            Position = 2,
                            Points = 6,
                            Team = new TeamViewModel
                            {
                                Id = Guid.NewGuid(),
                                Name = "Con Escudo",
                                Slug = "con-escudo",
                                LogoUrl = "/uploads/teams/crest.png"
                            }
                        },
                        new StandingsRowViewModel
                        {
                            Position = 3,
                            Points = 3,
                            Team = new TeamViewModel
                            {
                                Id = Guid.NewGuid(),
                                Name = "Placeholder",
                                Slug = "placeholder",
                                LogoUrl = "/assets/default-team.png"
                            }
                        },
                    }
                }
            }
        };

        var home = LeagueHomeComposer.Compose(
            new LeagueViewModel { Name = "Liga", Slug = "liga" },
            "Apertura",
            "apertura",
            new[] { new DivisionViewModel { Name = "A", Slug = "a" } },
            standings,
            null,
            null);

        var names = Assert.Single(home.DivisionPanels).Teams.Select(t => t.Name).ToList();
        Assert.Equal(new[] { "Con Escudo", "Sin Escudo", "Placeholder" }, names);
    }

    [Fact]
    public void CountUniqueTeams_DeduplicatesById()
    {
        var id = Guid.NewGuid();
        var standings = new SeasonGroupedViewModel<StandingsRowViewModel>
        {
            Divisions =
            {
                new DivisionGroupViewModel<StandingsRowViewModel>
                {
                    DivisionSlug = "a",
                    Data =
                    {
                        new StandingsRowViewModel { Position = 1, Team = new TeamViewModel { Id = id, Name = "Uno", Slug = "uno" } },
                    }
                },
                new DivisionGroupViewModel<StandingsRowViewModel>
                {
                    DivisionSlug = "b",
                    Data =
                    {
                        new StandingsRowViewModel { Position = 1, Team = new TeamViewModel { Id = id, Name = "Uno", Slug = "uno" } },
                        new StandingsRowViewModel { Position = 2, Team = new TeamViewModel { Id = Guid.NewGuid(), Name = "Dos", Slug = "dos" } },
                    }
                }
            }
        };

        Assert.Equal(2, LeagueHomeComposer.CountUniqueTeams(standings));
    }

    [Fact]
    public void BuildHeroStats_SeasonWideCountsDifferFromASingleDivisionSlice()
    {
        var all = Standings();
        var oneDivision = new SeasonGroupedViewModel<StandingsRowViewModel>
        {
            Divisions = { all.Divisions[0] }
        };
        var calendar = Calendar();

        var seasonWide = LeagueHomeComposer.BuildHeroStats(
            2, LeagueHomeComposer.CountUniqueTeams(all), calendar);
        var filtered = LeagueHomeComposer.BuildHeroStats(
            1, LeagueHomeComposer.CountUniqueTeams(oneDivision), calendar);

        Assert.Equal(2, seasonWide.DivisionCount);
        Assert.Equal(4, seasonWide.TeamCount);
        Assert.Equal(2, seasonWide.CurrentRound);
        Assert.True(filtered.TeamCount < seasonWide.TeamCount);
        Assert.Equal(1, filtered.DivisionCount);
    }

    [Theory]
    [InlineData("senior", "home-div-senior")]
    [InlineData("45-a", "home-div-d45-a")]
    [InlineData("A +45!", "home-div-A45")]
    [InlineData("", "home-div-x")]
    public void CssId_SanitizesSlug(string slug, string expected)
    {
        Assert.Equal(expected, LeagueHomeComposer.CssId("home-div-", slug));
    }

    [Fact]
    public void FormatWeekdayDayDeMonth_UsesSpanishLongForm()
    {
        var text = TeamDisplayHelper.FormatWeekdayDayDeMonth(new DateTime(2026, 8, 22));
        Assert.Contains("22", text);
        Assert.Contains("de", text);
        Assert.Contains("agosto", text, StringComparison.OrdinalIgnoreCase);
    }

    private static SeasonGroupedViewModel<StandingsRowViewModel> Standings() => new()
    {
        Divisions =
        {
            new DivisionGroupViewModel<StandingsRowViewModel>
            {
                DivisionName = "A",
                DivisionSlug = "a",
                Data =
                {
                    Row(1, "Belgrano", 6),
                    Row(2, "Pumas", 4),
                }
            },
            new DivisionGroupViewModel<StandingsRowViewModel>
            {
                DivisionName = "Senior",
                DivisionSlug = "senior",
                Data =
                {
                    Row(1, "Los Amigos", 9),
                    Row(2, "Forever", 3),
                }
            }
        }
    };

    private static SeasonGroupedViewModel<MatchdayGroupViewModel> Calendar() => new()
    {
        Divisions =
        {
            new DivisionGroupViewModel<MatchdayGroupViewModel>
            {
                DivisionName = "A",
                DivisionSlug = "a",
                DefaultRound = 2,
                Data =
                {
                    new MatchdayGroupViewModel
                    {
                        Round = 1,
                        Matches = { Match("A1", "A2", "Completed") }
                    },
                    new MatchdayGroupViewModel
                    {
                        Round = 2,
                        Matches =
                        {
                            Match("A3", "A4", "Completed"),
                            Match("A5", "A6", "Scheduled"),
                        }
                    }
                }
            },
            new DivisionGroupViewModel<MatchdayGroupViewModel>
            {
                DivisionName = "Senior",
                DivisionSlug = "senior",
                DefaultRound = 2,
                Data =
                {
                    new MatchdayGroupViewModel
                    {
                        Round = 2,
                        Matches =
                        {
                            Match("S1", "S2", "Scheduled"),
                            Match("S3", "S4", "Scheduled"),
                            Match("S5", "S6", "Scheduled"),
                        }
                    }
                }
            }
        }
    };

    private static StandingsRowViewModel Row(int pos, string name, int pts) => new()
    {
        Position = pos,
        Points = pts,
        Played = pos,
        Team = new TeamViewModel { Id = Guid.NewGuid(), Name = name, Slug = name.ToLowerInvariant().Replace(' ', '-') }
    };

    private static MatchViewModel Match(string home, string away, string status) => new()
    {
        Id = Guid.NewGuid(),
        Kickoff = new DateTime(2026, 8, 22, 14, 0, 0),
        Status = status,
        HomeTeam = new TeamViewModel { Name = home, Slug = home.ToLowerInvariant() },
        AwayTeam = new TeamViewModel { Name = away, Slug = away.ToLowerInvariant() },
        FieldName = "3"
    };
}
