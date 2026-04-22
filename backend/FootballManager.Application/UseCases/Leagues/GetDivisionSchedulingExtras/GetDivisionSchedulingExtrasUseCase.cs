using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.UseCases.Leagues.GetSchedulingEffectiveForDivision;

namespace FootballManager.Application.UseCases.Leagues.GetDivisionSchedulingExtras;

public sealed class GetDivisionSchedulingExtrasUseCase : IGetDivisionSchedulingExtrasUseCase
{
    private readonly IGetSchedulingEffectiveForDivisionUseCase _getSchedulingEffectiveForDivisionUseCase;

    public GetDivisionSchedulingExtrasUseCase(IGetSchedulingEffectiveForDivisionUseCase getSchedulingEffectiveForDivisionUseCase)
    {
        _getSchedulingEffectiveForDivisionUseCase = getSchedulingEffectiveForDivisionUseCase
                                                    ?? throw new ArgumentNullException(nameof(getSchedulingEffectiveForDivisionUseCase));
    }

    public async Task<DivisionSchedulingExtrasBundleResponse> ExecuteAsync(GetDivisionSchedulingExtrasRequest request, CancellationToken cancellationToken = default)
    {
        var detail = await _getSchedulingEffectiveForDivisionUseCase.ExecuteAsync(
            new GetSchedulingEffectiveForDivisionRequest(request.LeagueId, request.SeasonId, request.DivisionId, request.UserId),
            cancellationToken).ConfigureAwait(false);

        return new DivisionSchedulingExtrasBundleResponse
        {
            DivisionSeasonId = detail.DivisionSeasonId,
            GlobalMatchRule = detail.GlobalMatchRule,
            DivisionExtras = detail.DivisionExtras,
            ExplicitFieldIds = detail.ExplicitFieldIds,
            EffectivePreview = detail.Effective,
        };
    }
}
