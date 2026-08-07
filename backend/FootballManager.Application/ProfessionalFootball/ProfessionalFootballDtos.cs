namespace FootballManager.Application.ProfessionalFootball;

public sealed record CompetitionSummaryDto(
    string Slug,
    string Name,
    string Country,
    string? LogoUrl,
    int Season,
    string CurrentTournament,
    string? TournamentTypeId);

public sealed record CompetitionDetailDto(
    CompetitionSummaryDto Competition,
    IReadOnlyList<StandingGroupDto> Standings,
    IReadOnlyList<ProfessionalMatchDto> UpcomingMatches);

public sealed record StandingGroupDto(
    string Name,
    IReadOnlyList<StandingEntryDto> Entries);

public sealed record StandingEntryDto(
    int Position,
    string TeamExternalId,
    string TeamName,
    string? TeamLogo,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);

public sealed record TeamSummaryDto(
    string ExternalId,
    string Name,
    string? LogoUrl);

public sealed record ProfessionalMatchDto(
    string ExternalId,
    DateTimeOffset Date,
    string Status,
    string StatusDetail,
    TeamSummaryDto HomeTeam,
    TeamSummaryDto AwayTeam,
    string? Venue,
    int? HomeScore,
    int? AwayScore);

public sealed record CurrentTournament(
    int SeasonYear,
    string SeasonTypeId,
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool HasStandings);

public sealed record SeasonTypeInfo(
    string Id,
    string Name,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool? HasStandings);

public sealed record SeasonInfo(
    int Year,
    IReadOnlyList<SeasonTypeInfo> Types);
