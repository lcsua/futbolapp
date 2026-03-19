using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Api.Models.Public;
using FootballManager.Application.UseCases.Matches.GetMatchById;

namespace FootballManager.Api.Services.Public;

public class PublicMatchService
{
    private readonly IGetMatchByIdUseCase _getMatchByIdUseCase;

    public PublicMatchService(IGetMatchByIdUseCase getMatchByIdUseCase)
    {
        _getMatchByIdUseCase = getMatchByIdUseCase;
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
                HomeTeam = new TeamPublicDto { Id = res.HomeTeamId, Name = res.HomeTeamName },
                AwayTeam = new TeamPublicDto { Id = res.AwayTeamId, Name = res.AwayTeamName },
                Kickoff = DateTime.TryParse(res.MatchDate + " " + res.KickoffTime, out var dt) ? dt : DateTime.UtcNow
            };
        }
        catch { return null; }
    }
}
