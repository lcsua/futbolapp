namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public interface IPreviewFixtureImportUseCase
{
    Task<PreviewFixtureImportResponse> ExecuteAsync(PreviewFixtureImportRequest request, CancellationToken cancellationToken = default);
}
