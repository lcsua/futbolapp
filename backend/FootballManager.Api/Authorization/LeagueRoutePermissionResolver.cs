using FootballManager.Domain.Authorization;

namespace FootballManager.Api.Authorization
{
    public sealed class PermissionRequirement
    {
        public IReadOnlyList<string> AnyOf { get; }

        private PermissionRequirement(params string[] codes)
        {
            AnyOf = codes;
        }

        public static PermissionRequirement One(string code) => new(code);
        public static PermissionRequirement Any(params string[] codes) => new(codes);
    }

    public static class LeagueRoutePermissionResolver
    {
        public static PermissionRequirement? Resolve(string method, string path)
        {
            var p = (path ?? string.Empty).TrimEnd('/').ToLowerInvariant();
            var m = (method ?? "GET").ToUpperInvariant();

            if (p.StartsWith("/api/public") || p.StartsWith("/api/auth") || p == "/api/permissions")
                return null;

            if (!p.StartsWith("/api/leagues"))
                return null;

            var rest = p["/api/leagues".Length..].TrimStart('/');

            if (string.IsNullOrEmpty(rest))
                return m == "POST" ? PermissionRequirement.One(PermissionCodes.Leagues) : null;

            if (rest == "check-slug")
                return PermissionRequirement.One(PermissionCodes.Leagues);

            var slash = rest.IndexOf('/');
            var afterId = slash < 0 ? string.Empty : rest[(slash + 1)..];

            if (string.IsNullOrEmpty(afterId) || afterId == "my-access")
                return m == "PUT" ? PermissionRequirement.One(PermissionCodes.Leagues) : null;

            if (afterId.StartsWith("matches"))
                return PermissionRequirement.One(PermissionCodes.Matches);

            if (afterId.Contains("/standings") || afterId == "standings")
                return PermissionRequirement.One(PermissionCodes.Standings);

            if (afterId.Contains("fixtures"))
                return PermissionRequirement.One(PermissionCodes.Fixtures);

            if (afterId.StartsWith("users"))
                return PermissionRequirement.One(PermissionCodes.Users);

            if (afterId.StartsWith("roles"))
                return PermissionRequirement.One(PermissionCodes.Roles);

            if (afterId.StartsWith("competition-rules"))
                return PermissionRequirement.One(PermissionCodes.CompetitionRules);

            if (afterId.StartsWith("match-rules"))
                return PermissionRequirement.One(PermissionCodes.MatchRules);

            if (afterId.Contains("scheduling"))
                return PermissionRequirement.One(PermissionCodes.MatchRules);

            if (afterId.StartsWith("fields"))
                return PermissionRequirement.One(PermissionCodes.Fields);

            if (afterId.StartsWith("clubs"))
                return PermissionRequirement.One(PermissionCodes.Clubs);

            if (afterId.Contains("document"))
                return PermissionRequirement.One(PermissionCodes.Documents);

            if (afterId.StartsWith("team-name-aliases"))
                return PermissionRequirement.One(PermissionCodes.Teams);

            if (afterId.StartsWith("uploads"))
                return PermissionRequirement.Any(PermissionCodes.Leagues, PermissionCodes.Teams);

            if (afterId.StartsWith("teams"))
            {
                if (m == "GET" && afterId.Contains("players"))
                    return PermissionRequirement.Any(PermissionCodes.Teams, PermissionCodes.Matches);
                return PermissionRequirement.One(PermissionCodes.Teams);
            }

            if (afterId.StartsWith("seasons"))
            {
                if (afterId.Contains("standings"))
                    return PermissionRequirement.One(PermissionCodes.Standings);
                if (afterId.Contains("fixtures"))
                    return PermissionRequirement.One(PermissionCodes.Fixtures);
                if (afterId.Contains("setup") || afterId.Contains("copy-from") || afterId.Contains("assigned-team") || afterId.Contains("/divisions"))
                    return PermissionRequirement.One(PermissionCodes.SeasonSetup);
                if (m == "GET" && afterId == "seasons")
                    return null;
                return PermissionRequirement.One(PermissionCodes.Seasons);
            }

            if (afterId.StartsWith("divisions"))
            {
                if (m == "GET" && afterId == "divisions")
                    return null;
                return PermissionRequirement.One(PermissionCodes.Divisions);
            }

            if (m is "GET" or "HEAD")
                return null;

            return PermissionRequirement.One(PermissionCodes.Leagues);
        }
    }
}
