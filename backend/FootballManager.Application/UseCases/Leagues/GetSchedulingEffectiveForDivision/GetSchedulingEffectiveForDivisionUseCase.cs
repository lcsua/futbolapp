using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.GetSchedulingEffectiveForDivision;

public sealed class GetSchedulingEffectiveForDivisionUseCase : IGetSchedulingEffectiveForDivisionUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IMatchRulesResolver _matchRulesResolver;
    private readonly IMatchRuleRepository _matchRuleRepository;
    private readonly IDivisionMatchRulesRepository _divisionMatchRulesRepository;
    private readonly IDivisionSeasonFieldRepository _divisionSeasonFieldRepository;

    public GetSchedulingEffectiveForDivisionUseCase(
        IUserLeagueRepository userLeagueRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IMatchRulesResolver matchRulesResolver,
        IMatchRuleRepository matchRuleRepository,
        IDivisionMatchRulesRepository divisionMatchRulesRepository,
        IDivisionSeasonFieldRepository divisionSeasonFieldRepository)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _matchRulesResolver = matchRulesResolver ?? throw new ArgumentNullException(nameof(matchRulesResolver));
        _matchRuleRepository = matchRuleRepository ?? throw new ArgumentNullException(nameof(matchRuleRepository));
        _divisionMatchRulesRepository = divisionMatchRulesRepository ?? throw new ArgumentNullException(nameof(divisionMatchRulesRepository));
        _divisionSeasonFieldRepository = divisionSeasonFieldRepository ?? throw new ArgumentNullException(nameof(divisionSeasonFieldRepository));
    }

    public async Task<SchedulingEffectiveDetailResponse> ExecuteAsync(GetSchedulingEffectiveForDivisionRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var ds = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(request.SeasonId, request.DivisionId, cancellationToken);
        if (ds == null || ds.Season == null)
            throw new KeyNotFoundException("Division is not assigned to this season.");

        if (ds.Season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var matchRule = await _matchRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken)
                        ?? await _matchRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, null, cancellationToken);
        if (matchRule == null)
            throw new BusinessException("Match rules must be configured for this league or season.");

        var divisionRow = await _divisionMatchRulesRepository.GetByDivisionSeasonIdAsync(ds.Id, cancellationToken);
        var explicitFields = await _divisionSeasonFieldRepository.GetFieldIdsByDivisionSeasonIdAsync(ds.Id, cancellationToken);

        var effective = await _matchRulesResolver.GetEffectiveRulesAsync(ds.Id, cancellationToken).ConfigureAwait(false);

        return new SchedulingEffectiveDetailResponse
        {
            DivisionSeasonId = ds.Id,
            Effective = SchedulingDtoMapper.ToResponse(effective),
            GlobalMatchRule = ToMatchRuleSummary(matchRule),
            DivisionExtras = divisionRow == null ? null : ToDivisionExtras(divisionRow),
            ExplicitFieldIds = explicitFields,
        };
    }

    private static MatchRuleSchedulingSummaryDto ToMatchRuleSummary(MatchRule r) => new()
    {
        Id = r.Id,
        LeagueId = r.LeagueId,
        SeasonId = r.SeasonId,
        HalfMinutes = r.HalfMinutes,
        BreakMinutes = r.BreakMinutes,
        WarmupBufferMinutes = r.WarmupBufferMinutes,
        DerivedTotalMatchSlotMinutes = SlotBlockingCalculator.GetBaseDurationMinutes(r),
        SlotGranularityMinutes = r.SlotGranularityMinutes,
        FirstMatchToleranceMinutes = r.FirstMatchToleranceMinutes,
    };

    private static DivisionMatchRulesExtrasDto ToDivisionExtras(DivisionMatchRules e) => new()
    {
        DivisionSeasonId = e.DivisionSeasonId,
        HalfMinutes = e.HalfMinutes,
        BreakMinutes = e.BreakMinutes,
        WarmupBufferMinutes = e.WarmupBufferMinutes,
        SlotGranularityMinutes = e.SlotGranularityMinutes,
        FirstMatchToleranceMinutes = e.FirstMatchToleranceMinutes,
        BreakBetweenMatchesMinutes = e.BreakBetweenMatchesMinutes,
        AllowedTimeRangesJson = e.AllowedTimeRangesJson,
    };
}
