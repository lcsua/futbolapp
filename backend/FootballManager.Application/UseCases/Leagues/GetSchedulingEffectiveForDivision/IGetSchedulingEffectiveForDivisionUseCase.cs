using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;

namespace FootballManager.Application.UseCases.Leagues.GetSchedulingEffectiveForDivision;

public interface IGetSchedulingEffectiveForDivisionUseCase
{
    Task<SchedulingEffectiveDetailResponse> ExecuteAsync(GetSchedulingEffectiveForDivisionRequest request, CancellationToken cancellationToken = default);
}

public sealed class GetSchedulingEffectiveForDivisionRequest
{
    public GetSchedulingEffectiveForDivisionRequest(Guid leagueId, Guid seasonId, Guid divisionId, Guid userId)
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
