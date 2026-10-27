using FootballManager.Application.Dtos;
using FootballManager.Application.Helpers;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Tests;

public class FixtureDateShifterTests
{
    private static readonly DateOnly Cutoff = new(2026, 10, 24);

    [Theory]
    [InlineData(MatchStatus.SCHEDULED)]
    [InlineData(MatchStatus.POSTPONED)]
    [InlineData(MatchStatus.SUSPENDED)]
    public void Shifts_pending_matches_on_or_after_the_cutoff(MatchStatus status)
    {
        Assert.True(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, hasResult: false, status));
        Assert.True(FixtureDateShifter.ShouldShift(Cutoff.AddDays(7), Cutoff, hasResult: false, status));
    }

    [Fact]
    public void Leaves_earlier_dates_played_matches_and_closed_statuses()
    {
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff.AddDays(-1), Cutoff, false, MatchStatus.SCHEDULED));
        Assert.False(FixtureDateShifter.ShouldShift(null, Cutoff, false, MatchStatus.SCHEDULED));
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, hasResult: true, MatchStatus.SCHEDULED));
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, false, MatchStatus.PLAYED));
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, false, MatchStatus.COMPLETED));
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, false, MatchStatus.IN_PROGRESS));
        Assert.False(FixtureDateShifter.ShouldShift(Cutoff, Cutoff, false, MatchStatus.CANCELLED));
    }

    [Fact]
    public void Shift_moves_a_saturday_to_the_next_saturday()
    {
        var shifted = FixtureDateShifter.Shift(Cutoff);

        Assert.Equal(new DateOnly(2026, 10, 31), shifted);
        Assert.Equal(DayOfWeek.Saturday, shifted.DayOfWeek);
        Assert.Equal(DayOfWeek.Saturday, Cutoff.DayOfWeek);
    }

    [Fact]
    public void ShiftDraft_slides_the_cutoff_round_and_every_later_round_across_divisions()
    {
        var early = Match(new DateOnly(2026, 10, 17), "A");
        var washed = Match(Cutoff, "A");
        var washedOtherDivision = Match(Cutoff, "B");
        var later = Match(new DateOnly(2026, 10, 31), "B");
        var undated = Match(null, "A");
        var replan = new FixtureReplanDto(4, new[] { Guid.NewGuid() }, new[] { Guid.NewGuid() }, new[] { "aviso" });
        var draft = new FixtureDraftDto(new[]
        {
            Round(3, early.Date, new[] { early }),
            Round(4, Cutoff, new[] { washed, washedOtherDivision, undated }),
            Round(5, later.Date, new[] { later }),
        }, replan);

        var shifted = FixtureDateShifter.ShiftDraft(draft, Cutoff, out var updated);

        Assert.Equal(3, updated);
        Assert.Equal(early.Date, shifted.Rounds[0].MatchDate);
        Assert.Equal(early.Date, shifted.Rounds[0].Matches[0].Date);
        Assert.Equal(new DateOnly(2026, 10, 31), shifted.Rounds[1].MatchDate);
        Assert.Equal(new DateOnly(2026, 10, 31), shifted.Rounds[1].Matches[0].Date);
        Assert.Equal(new DateOnly(2026, 10, 31), shifted.Rounds[1].Matches[1].Date);
        Assert.Equal("B", shifted.Rounds[1].Matches[1].DivisionName);
        Assert.Null(shifted.Rounds[1].Matches[2].Date);
        Assert.Equal(washed.KickoffTime, shifted.Rounds[1].Matches[0].KickoffTime);
        Assert.Equal(new DateOnly(2026, 11, 7), shifted.Rounds[2].MatchDate);
        Assert.Same(replan, shifted.Replan);
    }

    private static FixtureDraftMatchDto Match(DateOnly? date, string division) =>
        new(
            Guid.NewGuid(),
            division,
            Guid.NewGuid(),
            "Local",
            Guid.NewGuid(),
            "Visitante",
            null,
            null,
            date,
            new TimeOnly(15, 0));

    private static FixtureDraftRoundDto Round(int number, DateOnly? date, IReadOnlyList<FixtureDraftMatchDto> matches) =>
        new(number, date, matches);
}
