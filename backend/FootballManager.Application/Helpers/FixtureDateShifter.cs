using FootballManager.Application.Dtos;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.Helpers;

/// <summary>
/// Moves kickoff dates forward one week from a cutoff, keeping the weekday.
/// Used when a match day is washed out and every later round has to slide.
/// </summary>
public static class FixtureDateShifter
{
    public const int ShiftDays = 7;

    public static bool ShouldShift(DateOnly? matchDate, DateOnly fromDate, bool hasResult, MatchStatus status)
    {
        if (matchDate is null || matchDate.Value < fromDate)
            return false;
        if (hasResult)
            return false;

        return status is MatchStatus.SCHEDULED or MatchStatus.POSTPONED or MatchStatus.SUSPENDED;
    }

    public static DateOnly Shift(DateOnly date) => date.AddDays(ShiftDays);

    public static FixtureDraftDto ShiftDraft(FixtureDraftDto draft, DateOnly fromDate, out int updatedMatches)
    {
        updatedMatches = 0;
        var rounds = new List<FixtureDraftRoundDto>(draft.Rounds.Count);

        foreach (var round in draft.Rounds)
        {
            var matches = new List<FixtureDraftMatchDto>(round.Matches.Count);
            foreach (var match in round.Matches)
            {
                if (match.Date is { } date && date >= fromDate)
                {
                    matches.Add(match with { Date = Shift(date) });
                    updatedMatches++;
                }
                else
                {
                    matches.Add(match);
                }
            }

            var roundDate = round.MatchDate is { } header && header >= fromDate
                ? Shift(header)
                : round.MatchDate;

            rounds.Add(new FixtureDraftRoundDto(round.RoundNumber, roundDate, matches, round.ByeTeams));
        }

        return new FixtureDraftDto(rounds, draft.Replan);
    }
}
