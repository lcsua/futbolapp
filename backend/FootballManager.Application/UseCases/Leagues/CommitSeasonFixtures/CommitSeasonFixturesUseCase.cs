using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Application.Services;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CommitSeasonFixtures;

public sealed class CommitSeasonFixturesUseCase : ICommitSeasonFixturesUseCase
{
    private readonly IUserLeagueRepository _userLeagueRepository;
    private readonly ILeagueRepository _leagueRepository;
    private readonly ISeasonRepository _seasonRepository;
    private readonly IDivisionSeasonRepository _divisionSeasonRepository;
    private readonly IFieldRepository _fieldRepository;
    private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
    private readonly IFixtureRepository _fixtureRepository;
    private readonly IFixtureDraftStore _draftStore;
    private readonly IUnitOfWork _unitOfWork;

    public CommitSeasonFixturesUseCase(
        IUserLeagueRepository userLeagueRepository,
        ILeagueRepository leagueRepository,
        ISeasonRepository seasonRepository,
        IDivisionSeasonRepository divisionSeasonRepository,
        IFieldRepository fieldRepository,
        ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
        IFixtureRepository fixtureRepository,
        IFixtureDraftStore draftStore,
        IUnitOfWork unitOfWork)
    {
        _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
        _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
        _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        _fieldRepository = fieldRepository ?? throw new ArgumentNullException(nameof(fieldRepository));
        _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
        _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
        _draftStore = draftStore ?? throw new ArgumentNullException(nameof(draftStore));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task ExecuteAsync(CommitSeasonFixturesRequest request, CancellationToken cancellationToken = default)
    {
        var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
        if (!hasAccess)
            throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

        var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
        if (season == null)
            throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
        if (season.LeagueId != request.LeagueId)
            throw new ForbiddenAccessException("Season does not belong to this league.");

        var draft = _draftStore.Get(request.SeasonId);
        if (draft == null || draft.Rounds.Count == 0)
            throw new BusinessException("No fixture draft to commit. Generate fixtures first.");

        var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
        if (league == null)
            throw new KeyNotFoundException($"League {request.LeagueId} not found.");
        await _fixtureRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var round in draft.Rounds)
        {
            foreach (var m in round.Matches)
            {
                var divisionSeason = await _divisionSeasonRepository.GetByIdAsync(m.DivisionSeasonId, cancellationToken);
                if (divisionSeason == null) continue;

                var homeTds = divisionSeason.TeamAssignments.FirstOrDefault(ta => ta.Id == m.HomeTeamDivisionSeasonId);
                var awayTds = divisionSeason.TeamAssignments.FirstOrDefault(ta => ta.Id == m.AwayTeamDivisionSeasonId);
                if (homeTds == null || awayTds == null) continue;

                if (!m.FieldId.HasValue || !m.Date.HasValue || !m.KickoffTime.HasValue) continue;

                var field = await _fieldRepository.GetByIdAsync(m.FieldId.Value, cancellationToken);
                if (field == null) continue;

                var fixture = new Fixture(
                    league,
                    season,
                    divisionSeason,
                    homeTds,
                    awayTds,
                    round.RoundNumber,
                    m.Date.Value,
                    m.KickoffTime.Value,
                    field);

                await _fixtureRepository.AddAsync(fixture, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _draftStore.Clear(request.SeasonId);
    }
}
