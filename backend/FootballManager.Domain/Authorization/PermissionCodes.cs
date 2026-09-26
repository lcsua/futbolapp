using System.Collections.Generic;

namespace FootballManager.Domain.Authorization
{
    public static class PermissionCodes
    {
        public const string Leagues = "leagues";
        public const string Seasons = "seasons";
        public const string SeasonSetup = "season_setup";
        public const string Divisions = "divisions";
        public const string Teams = "teams";
        public const string Clubs = "clubs";
        public const string Fields = "fields";
        public const string Fixtures = "fixtures";
        public const string Matches = "matches";
        public const string Standings = "standings";
        public const string CompetitionRules = "competition_rules";
        public const string MatchRules = "match_rules";
        public const string Users = "users";
        public const string Roles = "roles";
        public const string Documents = "documents";

        public static IReadOnlyList<(string Code, string Name, string Module)> Catalog { get; } =
        [
            (Leagues, "Leagues", "organization"),
            (Seasons, "Seasons", "organization"),
            (SeasonSetup, "Season setup", "organization"),
            (Divisions, "Divisions", "organization"),
            (Teams, "Teams", "organization"),
            (Clubs, "Clubs", "organization"),
            (Fields, "Fields", "organization"),
            (Fixtures, "Fixtures", "competition"),
            (Matches, "Matches", "competition"),
            (Standings, "Standings", "competition"),
            (CompetitionRules, "Competition rules", "settings"),
            (MatchRules, "Match rules", "settings"),
            (Users, "Users", "admin"),
            (Roles, "Roles", "admin"),
            (Documents, "Documents", "organization"),
        ];

        public static IReadOnlyList<string> AllCodes { get; } =
        [
            Leagues, Seasons, SeasonSetup, Divisions, Teams, Clubs, Fields,
            Fixtures, Matches, Standings, CompetitionRules, MatchRules,
            Users, Roles, Documents
        ];

        public static IReadOnlyList<string> CargaCodes { get; } = [Matches, Standings];
    }

    public static class RoleCodes
    {
        public const string Admin = "ADMIN";
        public const string Carga = "CARGA";
    }
}
