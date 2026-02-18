namespace FootballManager.Application.UseCases.Leagues.CopySeasonFrom
{
    public interface ICopySeasonFromUseCase
    {
        Task ExecuteAsync(CopySeasonFromRequest request, CancellationToken cancellationToken = default);
    }
}
