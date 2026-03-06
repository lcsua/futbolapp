using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;
using FootballManager.Domain.Enums;

namespace FootballManager.Application.UseCases.Matches.UpdateMatchResult;

public sealed class UpdateMatchResultUseCase : IUpdateMatchResultUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IResultRepository _resultRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMatchResultUseCase(
        IUserLeagueRepository userLeagueRepository,
        IFixtureRepository fixtureRepository,
        IResultRepository resultRepository,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _resultRepository = resultRepository ?? throw new ArgumentNullException(nameof(resultRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(Guid leagueId, Guid matchId, Guid userId, UpdateMatchResultRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(userId, leagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {leagueId}.");

        var fixture = await _fixtureRepository.GetByIdAsync(matchId, cancellationToken);
        if (fixture == null)
            throw new KeyNotFoundException($"Match {matchId} not found.");
        if (fixture.LeagueId != leagueId)
            throw new ForbiddenAccessException("Match does not belong to this league.");

        if (request.HomeScore < 0 || request.AwayScore < 0)
            throw new BusinessException("Scores cannot be negative.");

        // When both scores are provided, default status to Completed
        var status = request.HomeScore >= 0 && request.AwayScore >= 0
            ? MatchStatus.COMPLETED
            : ParseStatus(request.Status);
        if (status == MatchStatus.COMPLETED || status == MatchStatus.PLAYED)
        {
            var existingResult = await _resultRepository.GetByFixtureIdAsync(matchId, cancellationToken);
            if (existingResult != null)
            {
                existingResult.UpdateScore(request.HomeScore, request.AwayScore);
                _resultRepository.Update(existingResult);
            }
            else
            {
                var result = new Result(fixture, request.HomeScore, request.AwayScore);
                await _resultRepository.AddAsync(result, cancellationToken);
            }
        }

        fixture.ChangeStatus(status);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static MatchStatus ParseStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return MatchStatus.SCHEDULED;
        return Enum.TryParse<MatchStatus>(status, true, out var s) ? s : MatchStatus.SCHEDULED;
    }
}
