using System;
using System.Collections.Generic;
using System.Linq;
using FootballManager.Application.Helpers;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Helpers;

/// <summary>Resolve a CSV team name against division assignments (+ optional league aliases).</summary>
public static class TeamDivisionSeasonMatcher
{
    public static TeamDivisionSeason? Find(
        IEnumerable<TeamDivisionSeason> teamAssignments,
        string csvName,
        IReadOnlyDictionary<string, Guid>? aliasToTeamId = null)
    {
        var trimmed = csvName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;

        var list = teamAssignments as IList<TeamDivisionSeason> ?? teamAssignments.ToList();

        var exactName = list.FirstOrDefault(ta =>
            string.Equals(ta.Team.Name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
        if (exactName != null)
            return exactName;

        var exactDisplay = list.FirstOrDefault(ta =>
            string.Equals(ta.Team.DisplayName.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
        if (exactDisplay != null)
            return exactDisplay;

        var norm = TeamNameNormalizer.Normalize(trimmed);
        if (string.IsNullOrEmpty(norm))
            return null;

        var normalizedHit = list.FirstOrDefault(ta =>
            TeamNameNormalizer.Normalize(ta.Team.Name) == norm
            || TeamNameNormalizer.Normalize(ta.Team.DisplayName) == norm);
        if (normalizedHit != null)
            return normalizedHit;

        if (aliasToTeamId != null && aliasToTeamId.TryGetValue(norm, out var aliasedTeamId))
            return list.FirstOrDefault(ta => ta.TeamId == aliasedTeamId);

        return null;
    }
}
