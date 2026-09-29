using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Interfaces;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services;

public interface IRoundSlotAssignerFactory
{
    Task<RoundSlotAssigner> CreateAsync(Guid leagueId, IReadOnlyList<DivisionSeason> seasonDivisions, CancellationToken cancellationToken = default);
}

public sealed class RoundSlotAssignerFactory : IRoundSlotAssignerFactory
{
    private readonly IFieldRepository _fieldRepository;
    private readonly IFieldAvailabilityRepository _fieldAvailabilityRepository;
    private readonly IMatchRulesResolver _matchRulesResolver;

    public RoundSlotAssignerFactory(
        IFieldRepository fieldRepository,
        IFieldAvailabilityRepository fieldAvailabilityRepository,
        IMatchRulesResolver matchRulesResolver)
    {
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _fieldAvailabilityRepository = fieldAvailabilityRepository ?? throw new ArgumentNullException(nameof(fieldAvailabilityRepository));
        _matchRulesResolver = matchRulesResolver ?? throw new ArgumentNullException(nameof(matchRulesResolver));
    }

    public async Task<RoundSlotAssigner> CreateAsync(Guid leagueId, IReadOnlyList<DivisionSeason> seasonDivisions, CancellationToken cancellationToken = default)
    {
        var fields = (await _fieldRepository.GetByLeagueIdAsync(leagueId, cancellationToken)).Where(f => f.IsAvailable).ToList();
        IReadOnlyList<FieldAvailability> availabilities = fields.Count == 0
            ? Array.Empty<FieldAvailability>()
            : await _fieldAvailabilityRepository.GetByFieldIdsAsync(fields.Select(f => f.Id).ToList(), cancellationToken);

        var rulesCache = new Dictionary<Guid, EffectiveMatchRulesDto>();
        async Task<EffectiveMatchRulesDto> RulesFor(Guid divisionSeasonId)
        {
            if (!rulesCache.TryGetValue(divisionSeasonId, out var rules))
            {
                rules = await _matchRulesResolver.GetEffectiveRulesAsync(divisionSeasonId, cancellationToken).ConfigureAwait(false);
                rulesCache[divisionSeasonId] = rules;
            }
            return rules;
        }

        return new RoundSlotAssigner(fields, availabilities, seasonDivisions, RulesFor);
    }
}

/// <summary>
/// Assigns field and kickoff to matches added to an already scheduled date, respecting the field time
/// taken by the matches that stay on that date and by earlier calls on the same instance.
/// </summary>
public sealed class RoundSlotAssigner
{
    private readonly IReadOnlyDictionary<Guid, Field> _fieldById;
    private readonly IReadOnlyList<FieldAvailability> _availabilities;
    private readonly IReadOnlyList<DivisionSeason> _seasonDivisions;
    private readonly Func<Guid, Task<EffectiveMatchRulesDto>> _rulesFor;
    private readonly TeamFieldUsage _teamFieldUsage = new();
    private readonly Dictionary<DateOnly, List<(Guid FieldId, TimeOnly Start, int BlockMinutes)>> _plannedByDate = new();

    public RoundSlotAssigner(
        IEnumerable<Field> fields,
        IReadOnlyList<FieldAvailability> availabilities,
        IReadOnlyList<DivisionSeason> seasonDivisions,
        Func<Guid, Task<EffectiveMatchRulesDto>> rulesFor)
    {
        _fieldById = fields.Where(f => f.IsAvailable).ToDictionary(f => f.Id);
        _availabilities = availabilities;
        _seasonDivisions = seasonDivisions;
        _rulesFor = rulesFor;
    }

    public string? FieldName(Guid? fieldId) =>
        fieldId.HasValue && _fieldById.TryGetValue(fieldId.Value, out var field) ? field.Name : null;

    /// <summary>One slot per match (same order), or null when the date has no room for all of them.</summary>
    public async Task<IReadOnlyList<(Guid FieldId, TimeOnly Start)>?> AssignAsync(
        DateOnly date,
        IReadOnlyList<(DivisionSeason Ds, TeamDivisionSeason Home, TeamDivisionSeason Away)> matches,
        IEnumerable<Fixture> occupyingFixtures)
    {
        if (matches.Count == 0)
            return Array.Empty<(Guid, TimeOnly)>();
        if (_availabilities.Count == 0 || _fieldById.Count == 0)
            return null;

        var reserved = new List<(Guid FieldId, TimeOnly Start, int BlockMinutes)>();
        foreach (var f in occupyingFixtures.Where(f => f.MatchDate == date && f.FieldId.HasValue && f.StartTime.HasValue))
        {
            var rules = await _rulesFor(f.DivisionSeasonId);
            reserved.Add((f.FieldId!.Value, f.StartTime!.Value, rules.TotalMatchSlotBlockMinutes + rules.BreakBetweenMatchesMinutes));
        }
        if (_plannedByDate.TryGetValue(date, out var planned))
            reserved.AddRange(planned);

        var withRules = new List<(DivisionSeason Ds, TeamDivisionSeason Home, TeamDivisionSeason Away, EffectiveMatchRulesDto Rules)>();
        foreach (var (ds, home, away) in matches)
            withRules.Add((ds, home, away, await _rulesFor(ds.Id)));

        var slots = CrossDivisionFairMatchAssigner.Assign(
            withRules,
            date,
            (int)date.DayOfWeek,
            _availabilities,
            _fieldById,
            _teamFieldUsage,
            (divisionId, kickoff) => _seasonDivisions.FirstOrDefault(d => d.DivisionId == divisionId)?.Division.IsKickoffInBlockedWindow(kickoff) != true,
            null,
            reserved);
        if (slots == null)
            return null;

        if (!_plannedByDate.TryGetValue(date, out var list))
            _plannedByDate[date] = list = new List<(Guid, TimeOnly, int)>();
        for (var i = 0; i < slots.Count; i++)
            list.Add((slots[i].FieldId, slots[i].StartTime, withRules[i].Rules.TotalMatchSlotBlockMinutes + withRules[i].Rules.BreakBetweenMatchesMinutes));

        return slots.Select(s => (s.FieldId, s.StartTime)).ToList();
    }
}
