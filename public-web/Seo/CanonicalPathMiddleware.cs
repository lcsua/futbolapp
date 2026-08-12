using System.Text;

namespace PublicWeb.Seo;

/// <summary>
/// Redirects uppercase paths to lowercase (301) and strips trailing slashes (except root).
/// Does not touch query strings.
/// </summary>
public sealed class CanonicalPathMiddleware
{
    private readonly RequestDelegate _next;

    public CanonicalPathMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        // Skip static assets and health-ish paths.
        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/branding", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".map", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var normalized = path;
        var changed = false;

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
            changed = true;
        }

        if (HasUpperAscii(normalized))
        {
            normalized = normalized.ToLowerInvariant();
            changed = true;
        }

        if (changed && !string.Equals(path, normalized, StringComparison.Ordinal))
        {
            var target = context.Request.PathBase + normalized + context.Request.QueryString.Value;
            context.Response.Redirect(target, permanent: true);
            return;
        }

        await _next(context);
    }

    private static bool HasUpperAscii(string path)
    {
        foreach (var ch in path)
        {
            if (ch is >= 'A' and <= 'Z') return true;
        }
        return false;
    }
}
