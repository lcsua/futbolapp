using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FootballManager.Application.UseCases.Leagues.GenerateSeasonFixtures;

public sealed class GenerateSeasonFixturesUseCase : IGenerateSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly ICompetitionRuleRepository _competitionRuleRepository;
    private readonly IMatchRuleRepository _matchRuleRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly IFieldAvailabilityRepository _fieldAvailabilityRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly ILogger<GenerateSeasonFixturesUseCase> _logger;

    public GenerateSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        ICompetitionRuleRepository competitionRuleRepository,
        IMatchRuleRepository matchRuleRepository,
        IFieldRepository fieldRepository,
        IFieldAvailabilityRepository fieldAvailabilityRepository,
        IFixtureDraftStore draftStore,
        ILogger<GenerateSeasonFixturesUseCase> logger)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _competitionRuleRepository = competitionRuleRepository ?? throw new ArgumentNullException(nameof(competitionRuleRepository));
        _matchRuleRepository = matchRuleRepository ?? throw new ArgumentNullException(nameof(matchRuleRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _fieldAvailabilityRepository = fieldAvailabilityRepository ?? throw new ArgumentNullException(nameof(fieldAvailabilityRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GenerateSeasonFixturesResponse> ExecuteAsync(GenerateSeasonFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var competitionRule = await _competitionRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken)
            ?? await _competitionRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, null, cancellationToken);
        if (competitionRule == null)
            throw new BusinessException("Competition rules must be configured for this season (or at league level) before generating fixtures.");

        var matchRule = await _matchRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, request.SeasonId, cancellationToken)
            ?? await _matchRuleRepository.GetByLeagueAndSeasonAsync(request.LeagueId, null, cancellationToken);
        if (matchRule == null)
            throw new BusinessException("Match rules must be configured for this season (or at league level) before generating fixtures.");

        var divisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);
        if (request.DivisionId.HasValue)
            divisionSeasons = divisionSeasons.Where(ds => ds.DivisionId == request.DivisionId.Value).ToList();
        if (divisionSeasons.Count == 0)
            throw new BusinessException("No divisions configured for fixture generation with the selected filters.");

        var fields = await _fieldRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
        var availableFields = fields.Where(f => f.IsAvailable).ToList();

        if (availableFields.Count == 0)
            throw new BusinessException("At least one available field is required.");

        foreach (var ds in divisionSeasons)
        {
            var teamCount = ds.TeamAssignments.Count;
            if (teamCount < 2)
                throw new BusinessException($"Division '{ds.Division.Name}' must have at least 2 teams.");
        }

        var totalMatchDuration = matchRule.HalfMinutes * 2 + matchRule.BreakMinutes + matchRule.WarmupBufferMinutes;
        var slotScheduler = new FieldSlotScheduler(matchRule.SlotGranularityMinutes, totalMatchDuration);

        var fieldIds = availableFields.Select(f => f.Id).ToList();
        var allAvailabilities = await _fieldAvailabilityRepository.GetByFieldIdsAsync(fieldIds, cancellationToken);
        if (allAvailabilities.Count == 0)
            throw new BusinessException("Field availability must be configured for at least one field. Configure availability in Fields.");

        var matchDays = competitionRule.MatchDays.OrderBy(m => m.DayOfWeek).Select(m => m.DayOfWeek).ToList();
        if (matchDays.Count == 0)
            throw new BusinessException("At least one match day must be configured in competition rules.");

        var divisionsOrdered = divisionSeasons.OrderBy(ds => ds.Division.Name).ToList();
        var divisionById = divisionsOrdered
            .Select(ds => ds.Division)
            .DistinctBy(d => d.Id)
            .ToDictionary(d => d.Id);
        var allRounds = new List<FixtureDraftRoundDto>();
        var teamFieldUsage = new TeamFieldUsage();

        var maxRounds = 0;
        var divisionRounds = new Dictionary<Guid, IReadOnlyList<IReadOnlyList<(int Home, int Away)>>>();

        foreach (var ds in divisionsOrdered)
        {
            var teams = ds.TeamAssignments.OrderBy(ta => ta.Team.Name).ToList();
            var pairings = RoundRobinScheduler.Generate(teams.Count, competitionRule.IsHomeAway);
            divisionRounds[ds.Id] = pairings;
            maxRounds = Math.Max(maxRounds, pairings.Count);
        }

        for (var roundIndex = 0; roundIndex < maxRounds; roundIndex++)
        {
            var matchesThisRound = new List<(DivisionSeason Ds, TeamDivisionSeason Home, TeamDivisionSeason Away)>();

            var divCount = divisionsOrdered.Count;
            var firstDivIndex = (divCount - 1 - (roundIndex % divCount) + divCount) % divCount;
            var orderedDivs = new List<DivisionSeason>();
            for (var i = 0; i < divCount; i++)
            {
                var idx = (firstDivIndex - i + divCount) % divCount;
                orderedDivs.Add(divisionsOrdered[idx]);
            }

            foreach (var ds in orderedDivs)
            {
                var pairings = divisionRounds[ds.Id];
                if (roundIndex >= pairings.Count) continue;

                var teams = ds.TeamAssignments.OrderBy(ta => ta.Team.Name).ToList();
                foreach (var (homeIdx, awayIdx) in pairings[roundIndex])
                {
                    if (homeIdx >= teams.Count || awayIdx >= teams.Count || homeIdx == awayIdx)
                        continue;
                    matchesThisRound.Add((ds, teams[homeIdx], teams[awayIdx]));
                }
            }

            var matchDate = GetMatchDateForRound(season.StartDate, matchDays, roundIndex);
            var dayOfWeek = (int)matchDate.ToDateTime(TimeOnly.MinValue).DayOfWeek;
            var slots = slotScheduler.GenerateSlotsForMatchday(matchDate, dayOfWeek, allAvailabilities);

            if (slots.Count < matchesThisRound.Count)
                throw new BusinessException(
                    $"Not enough field availability to schedule all matches for round {roundIndex + 1} ({matchDate:yyyy-MM-dd}). " +
                    $"Required: {matchesThisRound.Count} slots, available: {slots.Count}. " +
                    "Configure field availability for the match day or reduce the number of matches.");

            bool IsKickoffSlotAllowed(Guid divisionId, TimeOnly kickoff)
            {
                if (!divisionById.TryGetValue(divisionId, out var division))
                    return true;
                return !division.IsKickoffInBlockedWindow(kickoff);
            }

            var matchInfos = matchesThisRound
                .Select(m => (m.Ds.DivisionId, m.Home.Team.Id, m.Away.Team.Id))
                .ToList();
            var assignments = slotScheduler.AssignMatchesToSlotsWithFairness(
                matchInfos,
                slots,
                teamFieldUsage,
                IsKickoffSlotAllowed,
                null);
            if (assignments == null)
                throw new BusinessException(
                    $"Could not assign all matches for round {roundIndex + 1} ({matchDate:yyyy-MM-dd}). " +
                    "Check field availability and division kickoff time restrictions (e.g. blocked hours for senior divisions).");

            var fieldById = availableFields.ToDictionary(f => f.Id);

            var draftMatches = new List<FixtureDraftMatchDto>();
            for (var m = 0; m < matchesThisRound.Count; m++)
            {
                var (ds, home, away) = matchesThisRound[m];
                var (assignFieldId, assignDate, kickoff) = assignments[m];
                var field = fieldById.GetValueOrDefault(assignFieldId);
                if (field == null)
                    throw new BusinessException($"Field {assignFieldId} not found.");

                _logger.LogInformation(
                    "Fixture generated: teamA={TeamA} teamB={TeamB} fieldId={FieldId} round={Round}",
                    home.Team.Name,
                    away.Team.Name,
                    assignFieldId,
                    roundIndex + 1);

                draftMatches.Add(new FixtureDraftMatchDto(
                    ds.Id,
                    ds.Division.Name,
                    home.Id,
                    home.Team.Name,
                    away.Id,
                    away.Team.Name,
                    field.Id,
                    field.Name,
                    assignDate,
                    kickoff
                ));
            }

            allRounds.Add(new FixtureDraftRoundDto(roundIndex + 1, matchDate, draftMatches));
        }

        LogFieldDistributionSummary(teamFieldUsage, availableFields);

        var draft = new FixtureDraftDto(allRounds);
        _draftStore.Set(request.SeasonId, draft);

        return new GenerateSeasonFixturesResponse(draft);
    }

    private static DateOnly GetFirstMatchDate(DateOnly seasonStart, int matchDayOfWeek)
    {
        var d = seasonStart.ToDateTime(TimeOnly.MinValue);
        var current = (int)d.DayOfWeek;
        var diff = (matchDayOfWeek - current + 7) % 7;
        return DateOnly.FromDateTime(d.AddDays(diff));
    }

    /// <summary>
    /// Get match date for a round. Supports multiple match days: round 0 = first match day,
    /// round 1 = second match day (if any), etc. Then repeats weekly.
    /// </summary>
    private static DateOnly GetMatchDateForRound(DateOnly seasonStart, IReadOnlyList<int> matchDays, int roundIndex)
    {
        var dayIndex = roundIndex % matchDays.Count;
        var weekIndex = roundIndex / matchDays.Count;
        var targetDay = matchDays[dayIndex];
        var firstDate = GetFirstMatchDate(seasonStart, targetDay);
        return firstDate.AddDays(7 * weekIndex);
    }

    private void LogFieldDistributionSummary(TeamFieldUsage teamFieldUsage, List<Field> availableFields)
    {
        var snapshot = teamFieldUsage.Snapshot();
        if (snapshot.Count == 0)
            return;

        var fieldNames = availableFields.ToDictionary(f => f.Id, f => f.Name);
        var byTeam = new Dictionary<Guid, List<(Guid FieldId, int Count)>>();
        foreach (var ((teamId, fieldId), count) in snapshot)
        {
            if (!byTeam.TryGetValue(teamId, out var list))
            {
                list = new List<(Guid, int)>();
                byTeam[teamId] = list;
            }
            list.Add((fieldId, count));
        }

        _logger.LogInformation("Field distribution summary: {TeamCount} teams with assignments", byTeam.Count);
        foreach (var (teamId, fieldCounts) in byTeam.OrderBy(x => x.Key))
        {
            var parts = fieldCounts
                .OrderByDescending(x => x.Count)
                .Select(x => $"{fieldNames.GetValueOrDefault(x.FieldId, x.FieldId.ToString())}={x.Count}");
            _logger.LogInformation("  Team {TeamId}: {Distribution}", teamId, string.Join(", ", parts));
        }
    }
}
