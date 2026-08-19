using PublicWeb.Helpers;
using PublicWeb.Models.Public;

namespace PublicWeb.Tests;

public class FixtureCalendarHelperTests
{
    [Fact]
    public void ResolveInitialFecha_SkipsDeferredEarlierRound_AfterLaterRoundWasPlayed()
    {
        var div = Division(
            Round(1, "SCHEDULED"),
            Round(2, "COMPLETED"),
            Round(3, "SCHEDULED"),
            Round(4, "SCHEDULED"));

        Assert.Equal(3, FixtureCalendarHelper.ResolveInitialFecha(div));
    }

    [Fact]
    public void ResolveInitialFecha_ReturnsDeferredRound_WhenItIsTheOnlyOpenLeft()
    {
        var div = Division(
            Round(1, "SCHEDULED"),
            Round(2, "COMPLETED"),
            Round(3, "COMPLETED"));

        Assert.Equal(1, FixtureCalendarHelper.ResolveInitialFecha(div));
    }

    [Fact]
    public void ResolveInitialFecha_StartsAtFirstOpen_WhenNothingHasBeenPlayed()
    {
        var div = Division(
            Round(1, "SCHEDULED"),
            Round(2, "SCHEDULED"));

        Assert.Equal(1, FixtureCalendarHelper.ResolveInitialFecha(div));
    }

    [Fact]
    public void ResolveInitialFecha_UsesLastPlayed_WhenSeasonIsComplete()
    {
        var div = Division(
            Round(1, "COMPLETED"),
            Round(2, "COMPLETED"));

        Assert.Equal(2, FixtureCalendarHelper.ResolveInitialFecha(div));
    }

    [Fact]
    public void MergeCalendar_SetsDefaultRoundPastDeferredFecha1()
    {
        var results = Season("Senior", Round(2, "COMPLETED"));
        var upcoming = Season(
            "Senior",
            Round(1, "SCHEDULED"),
            Round(3, "SCHEDULED"));

        var merged = FixtureCalendarHelper.MergeCalendar(results, upcoming);
        var senior = Assert.Single(merged.Divisions);

        Assert.Equal(new[] { 1, 2, 3 }, senior.Data.Select(d => d.Round));
        Assert.Equal(3, senior.DefaultRound);
    }

    [Fact]
    public void ResolveLeagueNextFecha_IgnoresOneDivisionDeferredRound()
    {
        var calendar = new SeasonGroupedViewModel<MatchdayGroupViewModel>
        {
            Divisions =
            {
                Division("A", Round(1, "COMPLETED"), Round(2, "COMPLETED"), Round(3, "SCHEDULED")),
                Division("Senior", Round(1, "SCHEDULED"), Round(2, "COMPLETED"), Round(3, "SCHEDULED")),
            }
        };

        Assert.Equal(3, FixtureCalendarHelper.ResolveLeagueNextFecha(calendar));
    }

    private static DivisionGroupViewModel<MatchdayGroupViewModel> Division(
        params MatchdayGroupViewModel[] rounds) =>
        Division("Senior", rounds);

    private static DivisionGroupViewModel<MatchdayGroupViewModel> Division(
        string name,
        params MatchdayGroupViewModel[] rounds) =>
        new()
        {
            DivisionName = name,
            DivisionSlug = name.ToLowerInvariant(),
            Data = rounds.ToList()
        };

    private static SeasonGroupedViewModel<MatchdayGroupViewModel> Season(
        string division,
        params MatchdayGroupViewModel[] rounds) =>
        new()
        {
            SeasonName = "Clausura 2026",
            SeasonSlug = "clausura-2026",
            Divisions = { Division(division, rounds) }
        };

    private static MatchdayGroupViewModel Round(int n, string status) =>
        new()
        {
            Round = n,
            Matches =
            {
                new MatchViewModel
                {
                    Id = Guid.NewGuid(),
                    Status = status,
                    Kickoff = new DateTime(2026, 8, n, 16, 0, 0)
                }
            }
        };
}
