namespace FootballManager.Application.ProfessionalFootball;

public interface IProfessionalFootballProvider
{
    Task<IReadOnlyList<SeasonInfo>> GetSeasonsAsync(string externalCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StandingGroupDto>> GetStandingsAsync(
        string externalCode,
        int season,
        string seasonType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfessionalMatchDto>> GetMatchesAsync(
        string externalCode,
        int season,
        string seasonType,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}
