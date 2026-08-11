using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Teams.UpsertTeamNameAliases;

public interface IUpsertTeamNameAliasesUseCase
{
    Task<UpsertTeamNameAliasesResponse> ExecuteAsync(
        UpsertTeamNameAliasesRequest request,
        CancellationToken cancellationToken = default);
}

public class UpsertTeamNameAliasesRequest
{
    public Guid LeagueId { get; set; }
    public Guid UserId { get; set; }
    public List<TeamNameAliasItemDto> Items { get; set; } = new();
}

public class TeamNameAliasItemDto
{
    public Guid TeamId { get; set; }
    public string Alias { get; set; } = string.Empty;
}

public class UpsertTeamNameAliasesResponse
{
    public int UpsertedCount { get; }

    public UpsertTeamNameAliasesResponse(int upsertedCount)
    {
        UpsertedCount = upsertedCount;
    }
}

public sealed class UpsertTeamNameAliasesUseCase : IUpsertTeamNameAliasesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ITeamRepository _teamRepository;
    private readonly ITeamNameAliasRepository _aliasRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertTeamNameAliasesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ITeamRepository teamRepository,
        ITeamNameAliasRepository aliasRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
        _aliasRepository = aliasRepository ?? throw new ArgumentNullException(nameof(aliasRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpsertTeamNameAliasesResponse> ExecuteAsync(
        UpsertTeamNameAliasesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken))
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken)
            ?? throw new KeyNotFoundException($"League {request.LeagueId} not found.");

        var upserted = 0;
        foreach (var item in request.Items ?? new List<TeamNameAliasItemDto>())
        {
            if (string.IsNullOrWhiteSpace(item.Alias))
                continue;

            var normalized = TeamNameNormalizer.Normalize(item.Alias);
            if (string.IsNullOrEmpty(normalized))
                continue;

            var team = await _teamRepository.GetByIdAsync(item.TeamId, cancellationToken);
            if (team == null || team.LeagueId != request.LeagueId)
                continue;

            // Skip if alias equals the canonical team name (no value).
            if (TeamNameNormalizer.EqualsNormalized(item.Alias, team.DisplayName) ||
                TeamNameNormalizer.EqualsNormalized(item.Alias, team.Name))
                continue;

            var existing = await _aliasRepository.GetByLeagueAndNormalizedAsync(
                request.LeagueId, normalized, cancellationToken);
            if (existing != null)
            {
                if (existing.TeamId != team.Id)
                    existing.ReassignTeam(team);
                existing.SetAlias(item.Alias.Trim(), normalized);
                _aliasRepository.Update(existing);
            }
            else
            {
                var alias = new TeamNameAlias(league, team, item.Alias.Trim(), normalized, "schedule-import");
                await _aliasRepository.AddAsync(alias, cancellationToken);
            }

            upserted++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpsertTeamNameAliasesResponse(upserted);
    }
}
