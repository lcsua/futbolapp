namespace FootballManager.Application.ProfessionalFootball;

public interface IProfessionalFootballAppService
{
    Task<IReadOnlyList<CompetitionSummaryDto>> GetCompetitionsAsync(CancellationToken cancellationToken = default);

    Task<CompetitionSummaryDto?> GetCompetitionSummaryAsync(string slug, CancellationToken cancellationToken = default);

    Task<CompetitionDetailDto?> GetCompetitionDetailAsync(string slug, CancellationToken cancellationToken = default);
}
