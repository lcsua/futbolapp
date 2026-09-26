using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Models.Public;
using FootballManager.Api.Services;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Api.Services.Public;

public class PublicTeamService
{
    private readonly ITeamRepository _teamRepository;

    public PublicTeamService(ITeamRepository teamRepository)
    {
        _teamRepository = teamRepository;
    }

    public async Task<TeamPublicDto?> GetTeamAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(slug, out var teamId)) return null;

        var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
        if (team == null) return null;

        return new TeamPublicDto
        {
            Id = team.Id,
            Name = team.CompetitionName,
            Slug = string.IsNullOrWhiteSpace(team.Slug) ? team.Id.ToString() : team.Slug,
            ShortName = team.ShortName,
            LogoUrl = team.EffectiveLogoUrl,
            LogoThumbUrl = LogoThumbnailService.DeriveThumbUrl(team.EffectiveLogoUrl)
        };
    }
}
