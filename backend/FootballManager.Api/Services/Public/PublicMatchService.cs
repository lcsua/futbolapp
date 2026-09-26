using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Models.Public;
using FootballManager.Api.Services;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.UseCases.Matches.GetMatchById;

namespace FootballManager.Api.Services.Public;

public class PublicMatchService
{
    private readonly IGetMatchByIdUseCase _getMatchByIdUseCase;
    private readonly IFixtureRepository _fixtureRepository;

    public PublicMatchService(
        IGetMatchByIdUseCase getMatchByIdUseCase,
        IFixtureRepository fixtureRepository)
    {
        _getMatchByIdUseCase = getMatchByIdUseCase;
        _fixtureRepository = fixtureRepository;
    }

    public async Task<MatchPublicDto?> GetMatchBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (!SlugGenerator.TryParseMatchSlug(slug, out var homeSlug, out var awayAndSeason))
            return null;

        var fixture = await _fixtureRepository.FindPublicByTeamSlugsAsync(homeSlug, awayAndSeason, cancellationToken);

        if (fixture == null)
            return null;

        return await GetMatchAsync(fixture.Id, cancellationToken);
    }

    public async Task<MatchPublicDto?> GetMatchAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: GetMatchByIdUseCase requires LeagueId for auth.
            // Since IsPublic is true, we can pass Guid.Empty for LeagueId and UserId.
            var req = new GetMatchByIdRequest { MatchId = matchId, LeagueId = Guid.Empty, UserId = Guid.Empty, IsPublic = true };
            var res = await _getMatchByIdUseCase.ExecuteAsync(req, cancellationToken);

            return new MatchPublicDto
            {
                Id = res.Id,
                Status = res.Status,
                HomeScore = res.HomeScore,
                AwayScore = res.AwayScore,
                LeagueSlug = res.LeagueSlug,
                SeasonSlug = res.SeasonSlug,
                HomeTeam = new TeamPublicDto
                {
                    Id = res.HomeTeamId,
                    Name = res.HomeTeamName,
                    Slug = res.HomeTeamSlug ?? string.Empty,
                    LogoUrl = res.HomeTeamLogoUrl,
                    LogoThumbUrl = LogoThumbnailService.DeriveThumbUrl(res.HomeTeamLogoUrl)
                },
                AwayTeam = new TeamPublicDto
                {
                    Id = res.AwayTeamId,
                    Name = res.AwayTeamName,
                    Slug = res.AwayTeamSlug ?? string.Empty,
                    LogoUrl = res.AwayTeamLogoUrl,
                    LogoThumbUrl = LogoThumbnailService.DeriveThumbUrl(res.AwayTeamLogoUrl)
                },
                Kickoff = DateTime.TryParse(res.MatchDate + " " + res.KickoffTime, out var dt) ? dt : default,
                FieldName = string.IsNullOrWhiteSpace(res.FieldName) ? null : res.FieldName.Trim(),
                RoundNumber = res.RoundNumber,
                DivisionName = string.IsNullOrWhiteSpace(res.DivisionName) ? null : res.DivisionName.Trim(),
                Incidents = (res.Incidents ?? Array.Empty<MatchIncidentDto>())
                    .Select(i => new MatchIncidentPublicDto
                    {
                        Minute = i.Minute,
                        TeamId = i.TeamId,
                        TeamName = i.TeamName ?? string.Empty,
                        PlayerName = i.PlayerName ?? string.Empty,
                        IncidentType = i.IncidentType ?? string.Empty,
                        Notes = i.Notes ?? string.Empty
                    })
                    .ToList()
            };
        }
        catch { return null; }
    }
}
