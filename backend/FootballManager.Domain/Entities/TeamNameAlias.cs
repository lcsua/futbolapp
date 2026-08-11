using System;
using FootballManager.Domain.Common;

namespace FootballManager.Domain.Entities;

/// <summary>
/// Alternate display/CSV names for a team within a league (improves import matching).
/// </summary>
public class TeamNameAlias : Entity
{
    public Guid LeagueId { get; private set; }
    public virtual League League { get; private set; } = null!;

    public Guid TeamId { get; private set; }
    public virtual Team Team { get; private set; } = null!;

    public string Alias { get; private set; } = string.Empty;
    public string NormalizedAlias { get; private set; } = string.Empty;
    public string Source { get; private set; } = "manual";

    protected TeamNameAlias() { }

    public TeamNameAlias(League league, Team team, string alias, string normalizedAlias, string source = "manual")
    {
        League = league ?? throw new ArgumentNullException(nameof(league));
        LeagueId = league.Id;
        Team = team ?? throw new ArgumentNullException(nameof(team));
        TeamId = team.Id;
        SetAlias(alias, normalizedAlias);
        Source = string.IsNullOrWhiteSpace(source) ? "manual" : source.Trim();
    }

    public void SetAlias(string alias, string normalizedAlias)
    {
        Alias = !string.IsNullOrWhiteSpace(alias)
            ? alias.Trim()
            : throw new ArgumentException("Alias cannot be empty.", nameof(alias));
        NormalizedAlias = !string.IsNullOrWhiteSpace(normalizedAlias)
            ? normalizedAlias.Trim()
            : throw new ArgumentException("Normalized alias cannot be empty.", nameof(normalizedAlias));
        UpdateTimestamp();
    }

    public void ReassignTeam(Team team)
    {
        Team = team ?? throw new ArgumentNullException(nameof(team));
        TeamId = team.Id;
        UpdateTimestamp();
    }
}
