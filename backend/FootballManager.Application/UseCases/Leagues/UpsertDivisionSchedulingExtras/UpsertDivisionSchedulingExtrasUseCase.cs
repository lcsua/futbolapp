using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.UpsertDivisionSchedulingExtras;

public sealed class UpsertDivisionSchedulingExtrasUseCase : IUpsertDivisionSchedulingExtrasUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IDivisionMatchRulesRepository _divisionMatchRulesRepository;
    private readonly IDivisionSeasonFieldRepository _divisionSeasonFieldRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMatchRulesResolver _matchRulesResolver;

    public UpsertDivisionSchedulingExtrasUseCase(
        IUserLeagueRepository userLeagueRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IDivisionMatchRulesRepository divisionMatchRulesRepository,
        IDivisionSeasonFieldRepository divisionSeasonFieldRepository,
        IFieldRepository fieldRepository,
        IUnitOfWork unitOfWork,
        IMatchRulesResolver matchRulesResolver)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _divisionMatchRulesRepository = divisionMatchRulesRepository ?? throw new ArgumentNullException(nameof(divisionMatchRulesRepository));
        _divisionSeasonFieldRepository = divisionSeasonFieldRepository ?? throw new ArgumentNullException(nameof(divisionSeasonFieldRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _matchRulesResolver = matchRulesResolver ?? throw new ArgumentNullException(nameof(matchRulesResolver));
    }

    public async Task ExecuteAsync(UpsertDivisionSchedulingExtrasRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var ds = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(request.SeasonId, request.DivisionId, cancellationToken);
        if (ds == null || ds.Season == null)
            throw new KeyNotFoundException("Division is not assigned to this season.");

        if (ds.Season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var divisionRuleEmpty = request.HalfMinutes == null
                                && request.BreakMinutes == null
                                && request.WarmupBufferMinutes == null
                                && request.SlotGranularityMinutes == null
                                && request.FirstMatchToleranceMinutes == null
                                && request.BreakBetweenMatchesMinutes == null
                                && request.AllowedTimeRangesJson == null;

        var trackedRules = await _divisionMatchRulesRepository.GetByDivisionSeasonIdTrackedAsync(ds.Id, cancellationToken);

        if (divisionRuleEmpty)
        {
            if (trackedRules != null)
                _divisionMatchRulesRepository.Remove(trackedRules);
        }
        else if (trackedRules == null)
        {
            var created = new DivisionMatchRules(
                ds,
                request.HalfMinutes,
                request.BreakMinutes,
                request.WarmupBufferMinutes,
                request.SlotGranularityMinutes,
                request.FirstMatchToleranceMinutes,
                request.BreakBetweenMatchesMinutes,
                request.AllowedTimeRangesJson);
            await _divisionMatchRulesRepository.AddAsync(created, cancellationToken);
        }
        else
        {
            trackedRules.Update(
                request.HalfMinutes,
                request.BreakMinutes,
                request.WarmupBufferMinutes,
                request.SlotGranularityMinutes,
                request.FirstMatchToleranceMinutes,
                request.BreakBetweenMatchesMinutes,
                request.AllowedTimeRangesJson);
            _divisionMatchRulesRepository.Update(trackedRules);
        }

        if (request.ExplicitFieldIds != null)
        {
            var distinctIds = request.ExplicitFieldIds.Distinct().ToList();
            var fields = new List<Field>(distinctIds.Count);
            foreach (var fieldId in distinctIds)
            {
                var field = await _fieldRepository.GetByIdAsync(fieldId, cancellationToken);
                if (field == null)
                    throw new KeyNotFoundException($"Field {fieldId} not found.");
                if (field.LeagueId != request.LeagueId)
                    throw new ForbiddenAccessException($"Field {fieldId} does not belong to this league.");
                fields.Add(field);
            }

            await _divisionSeasonFieldRepository.ReplaceFieldsAsync(ds, fields, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _matchRulesResolver.InvalidateCache(ds.Id);
    }
}
