using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.Services;

public interface ITeamNameAliasService
{
    /// <summary>Normalized alias → team id for the league.</summary>
    Task<IReadOnlyDictionary<string, Guid>> GetNormalizedLookupAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist a CSV/display alternate name for a team when it differs from Name/DisplayName.
    /// </summary>
    Task LearnAsync(
        League league,
        Guid teamId,
        string? csvName,
        string source,
        HashSet<(Guid TeamId, string Normalized)> learned,
        CancellationToken cancellationToken = default);
}

public sealed class TeamNameAliasService : ITeamNameAliasService
{
    private readonly ITeamNameAliasRepository _aliasRepository;
    private readonly ITeamRepository _teamRepository;

    public TeamNameAliasService(
        ITeamNameAliasRepository aliasRepository,
        ITeamRepository teamRepository)
    {
        _aliasRepository = aliasRepository ?? throw new ArgumentNullException(nameof(aliasRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetNormalizedLookupAsync(
        Guid leagueId,
        CancellationToken cancellationToken = default)
    {
        var aliases = await _aliasRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
        return aliases
            .GroupBy(a => a.NormalizedAlias, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().TeamId, StringComparer.Ordinal);
    }

    public async Task LearnAsync(
        League league,
        Guid teamId,
        string? csvName,
        string source,
        HashSet<(Guid TeamId, string Normalized)> learned,
        CancellationToken cancellationToken = default)
    {
        if (league == null) throw new ArgumentNullException(nameof(league));
        if (string.IsNullOrWhiteSpace(csvName))
            return;

        var normalized = TeamNameNormalizer.Normalize(csvName);
        if (string.IsNullOrEmpty(normalized))
            return;

        if (!learned.Add((teamId, normalized)))
            return;

        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team == null || team.LeagueId != league.Id)
            return;

        if (TeamNameNormalizer.EqualsNormalized(csvName, team.DisplayName) ||
            TeamNameNormalizer.EqualsNormalized(csvName, team.Name))
            return;

        var existing = await _aliasRepository.GetByLeagueAndNormalizedAsync(league.Id, normalized, cancellationToken);
        if (existing != null)
        {
            if (existing.TeamId != team.Id)
                existing.ReassignTeam(team);
            existing.SetAlias(csvName.Trim(), normalized);
            _aliasRepository.Update(existing);
            return;
        }

        var alias = new TeamNameAlias(
            league,
            team,
            csvName.Trim(),
            normalized,
            string.IsNullOrWhiteSpace(source) ? "import" : source.Trim());
        await _aliasRepository.AddAsync(alias, cancellationToken);
    }
}
