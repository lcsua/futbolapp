using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;

namespace FootballManager.Application.UseCases.Leagues.DeleteSeason;

public sealed class DeleteSeasonUseCase : IDeleteSeasonUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly ICompetitionRuleRepository _competitionRuleRepository;
    private readonly IMatchRuleRepository _matchRuleRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteSeasonUseCase(
        IUserLeagueRepository userLeagueRepository,
        ISeasonRepository seasonRepository,
        IFixtureRepository fixtureRepository,
        ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        ICompetitionRuleRepository competitionRuleRepository,
        IMatchRuleRepository matchRuleRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _competitionRuleRepository = competitionRuleRepository ?? throw new ArgumentNullException(nameof(competitionRuleRepository));
        _matchRuleRepository = matchRuleRepository ?? throw new ArgumentNullException(nameof(matchRuleRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(DeleteSeasonRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        // Leaf → root hard delete (FK Restrict prevents cascade from Season).
        await _fixtureRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _teamDivisionSeasonRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _competitionRuleRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _matchRuleRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Cascades DivisionSeasonField + DivisionMatchRules.
        await _divisionSeasonRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _seasonRepository.RemoveByIdAsync(request.SeasonId, cancellationToken);

        _draftStore.Clear(request.SeasonId);
    }
}
