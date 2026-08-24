using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

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

    public static string? CookieDomain(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        if (host.Equals("miliga.com.ar", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("www.miliga.com.ar", StringComparison.OrdinalIgnoreCase))
        {
            return "miliga.com.ar";
        }

        return null;
    }

    public static CookieOptions CreateCookieOptions(HttpRequest request)
    {
        return new CookieOptions
        {
            Path = "/",
            HttpOnly = true,
            Secure = request.IsHttps || CookieDomain(request.Host.Host) is not null,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
            Domain = CookieDomain(request.Host.Host)
        };
    }

    public static void SetCookie(HttpResponse response, HttpRequest request, string name, string? value)
    {
        var options = CreateCookieOptions(request);
        if (string.IsNullOrWhiteSpace(value) || !IsValidPath(value))
        {
            response.Cookies.Delete(name, options);
            return;
        }

        response.Cookies.Append(name, value, options);
    }
}

public sealed class HomeLeaguePreferenceRequest
{
    public string? Slug { get; set; }
}
