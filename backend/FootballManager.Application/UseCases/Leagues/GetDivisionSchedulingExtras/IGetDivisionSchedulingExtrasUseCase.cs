using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetDivisionSchedulingExtras;

public interface IGetDivisionSchedulingExtrasUseCase
{
    Task<DivisionSchedulingExtrasBundleResponse> ExecuteAsync(GetDivisionSchedulingExtrasRequest request, CancellationToken cancellationToken = default);
}

public sealed class GetDivisionSchedulingExtrasRequest
{
    public GetDivisionSchedulingExtrasRequest(Guid leagueId, Guid seasonId, Guid divisionId, Guid userId)
    {
        LeagueId = leagueId;
        SeasonId = seasonId;
        DivisionId = divisionId;
        UserId = userId;
    }

    public Guid LeagueId { get; }
    public Guid SeasonId { get; }
    public Guid DivisionId { get; }
    public Guid UserId { get; }
}
