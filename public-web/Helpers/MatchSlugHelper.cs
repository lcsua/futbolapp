using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PublicWeb.Models.Public;

namespace PublicWeb.Helpers;

/// <summary>
/// Public match URLs: {home-slug}-vs-{away-slug} plus optional -{season-slug}.
/// Kept in public-web so pages do not depend on the backend project.
/// </summary>
public static class MatchSlugHelper
{
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleHyphens = new(@"-+", RegexOptions.Compiled);

    public static string FromMatch(MatchViewModel match)
    {
        var home = TeamToken(match.HomeTeam);
        var away = TeamToken(match.AwayTeam);
        if (string.IsNullOrEmpty(home) || string.IsNullOrEmpty(away))
            return match.Id.ToString();

        var slug = $"{home}-vs-{away}";
        var season = Normalize(match.SeasonSlug);
        if (!string.IsNullOrEmpty(season))
            slug += $"-{season}";
        return slug;
    }

    public static string AppRelative(MatchViewModel match) => "~/partido/" + FromMatch(match);

    public static string Path(MatchViewModel match) => "/partido/" + FromMatch(match);

    public static void ApplySeasonSlug(MatchViewModel match, string? seasonSlug)
    {
        if (string.IsNullOrWhiteSpace(match.SeasonSlug) && !string.IsNullOrWhiteSpace(seasonSlug))
            match.SeasonSlug = seasonSlug;
    }

    public static void ApplySeasonSlug(SeasonGroupedViewModel<MatchdayGroupViewModel>? grouped)
    {
        if (grouped == null || string.IsNullOrWhiteSpace(grouped.SeasonSlug))
            return;

        foreach (var division in grouped.Divisions)
        {
            foreach (var day in division.Data)
            {
                foreach (var match in day.Matches)
                    ApplySeasonSlug(match, grouped.SeasonSlug);
            }
        }
    }

    public static string FromNames(string? home, string? away, string? season = null)
    {
        var homeSlug = Normalize(home);
        var awaySlug = Normalize(away);
        if (string.IsNullOrEmpty(homeSlug) || string.IsNullOrEmpty(awaySlug))
            return string.Empty;

        var slug = $"{homeSlug}-vs-{awaySlug}";
        var seasonSlug = Normalize(season);
        if (!string.IsNullOrEmpty(seasonSlug))
            slug += $"-{seasonSlug}";
        return slug;
    }

    private static string TeamToken(TeamViewModel? team)
    {
        if (team == null) return string.Empty;
        if (IsUsableTeamSlug(team.Slug))
            return Normalize(team.Slug);
        return Normalize(team.Name);
    }

    private static bool IsUsableTeamSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return false;
        return !Guid.TryParse(slug, out _);
    }

    private static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-')
                sb.Append('-');
        }

        var result = InvalidChars.Replace(sb.ToString(), string.Empty);
        result = MultipleHyphens.Replace(result, "-");
        return result.Trim('-');
    }
}
