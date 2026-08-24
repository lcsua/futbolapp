using System.Text.RegularExpressions;

namespace PublicWeb.Helpers;

public static partial class HomeLeaguePreference
{
    public const string PinnedCookie = "miliga-home-league";
    public const string LastCookie = "miliga-last-league";

    [GeneratedRegex(@"^(argentina/)?[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex LeaguePathRegex();

    public static bool IsValidPath(string? value) =>
        !string.IsNullOrWhiteSpace(value) && LeaguePathRegex().IsMatch(value);

    public static string? Resolve(string? pinned, string? last)
    {
        if (IsValidPath(pinned)) return pinned;
        if (IsValidPath(last)) return last;
        return null;
    }

    public static string ToPublicUrl(string path) => $"/ligas/{path}";
}
