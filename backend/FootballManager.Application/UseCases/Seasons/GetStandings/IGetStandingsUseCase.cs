namespace FootballManager.Application.UseCases.Seasons.GetStandings;

public interface IGetStandingsUseCase
{
    Task<GetStandingsResponse> ExecuteAsync(GetStandingsRequest request, CancellationToken cancellationToken = default);
}
