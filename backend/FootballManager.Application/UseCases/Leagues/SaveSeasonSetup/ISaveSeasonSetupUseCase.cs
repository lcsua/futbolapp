namespace FootballManager.Application.UseCases.Leagues.SaveSeasonSetup
{
    public interface ISaveSeasonSetupUseCase
    {
        Task ExecuteAsync(SaveSeasonSetupRequest request, CancellationToken cancellationToken = default);
    }
}
