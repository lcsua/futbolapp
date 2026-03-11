namespace FootballManager.Application.UseCases.Leagues.ImportFixtures;

public sealed class ImportFixturesResponse
{
    public int ImportedCount { get; }
    public IReadOnlyList<string> Errors { get; }

    public ImportFixturesResponse(int importedCount, IReadOnlyList<string> errors)
    {
        ImportedCount = importedCount;
        Errors = errors;
    }

    public static ImportFixturesResponse Success(int count) => new(count, Array.Empty<string>());
    public static ImportFixturesResponse WithErrors(IReadOnlyList<string> errors) => new(0, errors);
}
