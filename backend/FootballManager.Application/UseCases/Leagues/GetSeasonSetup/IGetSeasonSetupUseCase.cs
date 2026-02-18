namespace FootballManager.Application.UseCases.Leagues.GetSeasonSetup
{
    public interface IGetSeasonSetupUseCase
    {
        Task<GetSeasonSetupResponse> ExecuteAsync(GetSeasonSetupRequest request, CancellationToken cancellationToken = default);
    }
}
