namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public interface IImportFixturesUseCase
{
    Task<ImportFixturesResponse> ExecuteAsync(ImportFixturesRequest request, CancellationToken cancellationToken = default);
}
