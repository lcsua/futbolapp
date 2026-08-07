namespace FootballManager.Application.ProfessionalFootball;

/// <summary>
/// Resolves the active ESPN season type (e.g. Torneo Clausura) from season metadata.
/// </summary>
public static class CurrentTournamentResolver
{
    public static CurrentTournament? Resolve(IReadOnlyList<SeasonInfo> seasons, DateTimeOffset nowUtc)
    {
        if (seasons == null || seasons.Count == 0)
            return null;

        var yearSeason = seasons
            .Where(s => s.Year == nowUtc.Year)
            .OrderByDescending(s => s.Year)
            .FirstOrDefault()
            ?? seasons.OrderByDescending(s => s.Year).First();

        if (yearSeason.Types.Count == 0)
            return null;

        var active = yearSeason.Types
            .Where(t => nowUtc >= t.StartDate && nowUtc <= t.EndDate)
            .ToList();

        var preferred = PreferStandingsEligible(active)
            ?? PreferStandingsEligible(yearSeason.Types
                .Where(t => t.StartDate <= nowUtc)
                .OrderByDescending(t => t.StartDate)
                .ToList())
            ?? PreferStandingsEligible(yearSeason.Types.ToList())
            ?? active.FirstOrDefault()
            ?? yearSeason.Types.OrderByDescending(t => t.StartDate).First();

        return ToTournament(yearSeason.Year, preferred);
    }

    /// <summary>
    /// Type to use for standings: prefer active types that look like league phases with tables.
    /// </summary>
    public static CurrentTournament? ResolveForStandings(IReadOnlyList<SeasonInfo> seasons, DateTimeOffset nowUtc)
    {
        if (seasons == null || seasons.Count == 0)
            return null;

        var yearSeason = seasons
            .Where(s => s.Year == nowUtc.Year)
            .OrderByDescending(s => s.Year)
            .FirstOrDefault()
            ?? seasons.OrderByDescending(s => s.Year).First();

        var active = yearSeason.Types
            .Where(t => nowUtc >= t.StartDate && nowUtc <= t.EndDate)
            .ToList();

        var fromActive = PreferStandingsEligible(active);
        if (fromActive != null)
            return ToTournament(yearSeason.Year, fromActive);

        // Playoffs-only window: fall back to the most recent league-table phase that already started.
        var fallback = PreferStandingsEligible(
            yearSeason.Types
                .Where(t => t.StartDate <= nowUtc && IsLeagueTablePhase(t))
                .OrderByDescending(t => t.StartDate)
                .ToList());

        return fallback != null ? ToTournament(yearSeason.Year, fallback) : Resolve(seasons, nowUtc);
    }

    private static SeasonTypeInfo? PreferStandingsEligible(IReadOnlyList<SeasonTypeInfo> candidates)
    {
        if (candidates.Count == 0)
            return null;

        var withFlag = candidates.Where(t => t.HasStandings == true).ToList();
        if (withFlag.Count > 0)
            return PreferLeagueName(withFlag) ?? withFlag[0];

        var leaguePhases = candidates.Where(IsLeagueTablePhase).ToList();
        if (leaguePhases.Count > 0)
            return PreferLeagueName(leaguePhases) ?? leaguePhases[0];

        // Do not fall back to playoff/cup rounds for standings selection.
        return null;
    }

    private static SeasonTypeInfo? PreferLeagueName(IReadOnlyList<SeasonTypeInfo> candidates) =>
        candidates.FirstOrDefault(t =>
            t.Name.StartsWith("Torneo ", StringComparison.OrdinalIgnoreCase));

    private static bool IsLeagueTablePhase(SeasonTypeInfo t)
    {
        if (t.HasStandings == true)
            return true;
        if (t.HasStandings == false)
            return false;
        // ESPN seasons metadata often omits hasStandings; "Torneo X" are the group-stage tables.
        return t.Name.StartsWith("Torneo ", StringComparison.OrdinalIgnoreCase);
    }

    private static CurrentTournament ToTournament(int year, SeasonTypeInfo t) =>
        new(year, t.Id, t.Name, t.StartDate, t.EndDate, t.HasStandings == true || IsLeagueTablePhase(t));
}
