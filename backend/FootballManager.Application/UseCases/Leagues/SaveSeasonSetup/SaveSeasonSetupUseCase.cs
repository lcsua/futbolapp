using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.SaveSeasonSetup
{
    public class SaveSeasonSetupUseCase : ISaveSeasonSetupUseCase
    {
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly ITeamDivisionSeasonRepository _teamDivisionSeasonRepository;
        private readonly IFixtureRepository _fixtureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SaveSeasonSetupUseCase(
            IUserLeagueRepository userLeagueRepository,
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            ITeamRepository teamRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
            IFixtureRepository fixtureRepository,
            IUnitOfWork unitOfWork)
        {
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
            _fixtureRepository = fixtureRepository ?? throw new ArgumentNullException(nameof(fixtureRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(SaveSeasonSetupRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            SeasonGuard.EnsureOpen(season);

            var divisions = request.Divisions ?? new List<SaveSeasonSetupDivisionDto>();
            var allTeamIds = new HashSet<Guid>();
            foreach (var div in divisions)
            {
                foreach (var tid in div.TeamIds)
                {
                    if (!allTeamIds.Add(tid))
                        throw new BusinessException($"Team {tid} cannot be assigned to more than one division in the same season.");
                }
            }

            var existingDivisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(
                request.SeasonId, cancellationToken);
            var existingByDivisionId = existingDivisionSeasons.ToDictionary(ds => ds.DivisionId);

            var lockedDivisionIds = new HashSet<Guid>();
            foreach (var ds in existingDivisionSeasons)
            {
                var count = await _fixtureRepository.CountByDivisionSeasonIdAsync(ds.Id, cancellationToken);
                if (count > 0)
                    lockedDivisionIds.Add(ds.DivisionId);
            }

            var teamIdsWithFixtures = await _fixtureRepository.GetTeamIdsWithFixturesAsync(request.SeasonId, cancellationToken);
            var requestedByDivisionId = divisions.ToDictionary(d => d.DivisionId, d => d.TeamIds.ToHashSet());

            // Validate every removal before touching anything.
            foreach (var ds in existingDivisionSeasons)
            {
                var requestedTeams = requestedByDivisionId.GetValueOrDefault(ds.DivisionId) ?? new HashSet<Guid>();
                var existingTeams = ds.TeamAssignments.Select(ta => ta.TeamId).ToHashSet();
                if (existingTeams.SetEquals(requestedTeams))
                    continue;

                var divisionName = ds.Division?.Name ?? ds.DivisionId.ToString();
                if (lockedDivisionIds.Contains(ds.DivisionId) && !request.AllowChangesToLockedDivisions)
                    throw new BusinessException(
                        $"La división \"{divisionName}\" tiene fixture guardado. Confirmá el cambio de equipos para continuar.");

                var removedWithFixtures = ds.TeamAssignments
                    .Where(ta => !requestedTeams.Contains(ta.TeamId) && teamIdsWithFixtures.Contains(ta.TeamId))
                    .Select(ta => ta.Team?.DisplayName ?? ta.TeamId.ToString())
                    .ToList();
                if (removedWithFixtures.Count > 0)
                    throw new BusinessException(
                        $"No se puede sacar de \"{divisionName}\" a {string.Join(", ", removedWithFixtures)}: ya tiene partidos en el fixture.");
            }

            // Incremental: keep the existing assignments (fixtures reference them), remove only the teams that
            // leave and add only the new ones.
            foreach (var ds in existingDivisionSeasons)
            {
                var requestedTeams = requestedByDivisionId.GetValueOrDefault(ds.DivisionId) ?? new HashSet<Guid>();
                foreach (var ta in ds.TeamAssignments.Where(ta => !requestedTeams.Contains(ta.TeamId)).ToList())
                    await _teamDivisionSeasonRepository.RemoveByTeamAndDivisionSeasonAsync(ta.TeamId, ds.Id, cancellationToken);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var divDto in divisions)
            {
                existingByDivisionId.TryGetValue(divDto.DivisionId, out var divisionSeason);
                var existingTeams = divisionSeason?.TeamAssignments.Select(ta => ta.TeamId).ToHashSet() ?? new HashSet<Guid>();
                var addedTeamIds = divDto.TeamIds.Where(id => !existingTeams.Contains(id)).Distinct().ToList();
                if (addedTeamIds.Count == 0)
                    continue;

                if (divisionSeason == null)
                {
                    var division = await _divisionRepository.GetByIdAsync(divDto.DivisionId, cancellationToken);
                    if (division == null)
                        throw new KeyNotFoundException($"Division {divDto.DivisionId} not found.");
                    if (division.LeagueId != request.LeagueId)
                        throw new ForbiddenAccessException("Division does not belong to this league.");

                    divisionSeason = new DivisionSeason(season, division);
                    await _divisionSeasonRepository.AddAsync(divisionSeason, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    existingByDivisionId[divDto.DivisionId] = divisionSeason;
                }

                foreach (var teamId in addedTeamIds)
                {
                    var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
                    if (team == null)
                        throw new KeyNotFoundException($"Team {teamId} not found.");
                    if (team.LeagueId != request.LeagueId)
                        throw new ForbiddenAccessException("Team does not belong to this league.");
                    await _teamDivisionSeasonRepository.AddAsync(new TeamDivisionSeason(team, divisionSeason), cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
