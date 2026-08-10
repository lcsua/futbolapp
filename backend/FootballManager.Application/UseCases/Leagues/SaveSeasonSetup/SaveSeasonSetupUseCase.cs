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

            // Locked divisions with fixtures: allow save only if team set is unchanged.
            foreach (var lockedDivisionId in lockedDivisionIds)
            {
                var existingTeams = existingByDivisionId[lockedDivisionId].TeamAssignments
                    .Select(ta => ta.TeamId)
                    .OrderBy(id => id)
                    .ToList();
                var requested = divisions.FirstOrDefault(d => d.DivisionId == lockedDivisionId);
                var requestedTeams = (requested?.TeamIds ?? new List<Guid>())
                    .OrderBy(id => id)
                    .ToList();

                if (!existingTeams.SequenceEqual(requestedTeams))
                {
                    var name = existingByDivisionId[lockedDivisionId].Division?.Name ?? lockedDivisionId.ToString();
                    throw new BusinessException(
                        $"Cannot modify teams for division \"{name}\": fixtures have been committed for that division.");
                }
            }

            // Update only unlocked divisions (and create new ones). Locked stay as-is.
            foreach (var divDto in divisions)
            {
                if (lockedDivisionIds.Contains(divDto.DivisionId))
                    continue;

                var division = await _divisionRepository.GetByIdAsync(divDto.DivisionId, cancellationToken);
                if (division == null)
                    throw new KeyNotFoundException($"Division {divDto.DivisionId} not found.");
                if (division.LeagueId != request.LeagueId)
                    throw new ForbiddenAccessException("Division does not belong to this league.");

                if (!existingByDivisionId.TryGetValue(divDto.DivisionId, out var divisionSeason))
                {
                    if (divDto.TeamIds.Count == 0)
                        continue;

                    divisionSeason = new DivisionSeason(season, division);
                    await _divisionSeasonRepository.AddAsync(divisionSeason, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    existingByDivisionId[divDto.DivisionId] = divisionSeason;
                }
                else
                {
                    await _teamDivisionSeasonRepository.RemoveByDivisionSeasonIdAsync(
                        divisionSeason.Id, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                foreach (var teamId in divDto.TeamIds)
                {
                    var team = await _teamRepository.GetByIdAsync(teamId, cancellationToken);
                    if (team == null)
                        throw new KeyNotFoundException($"Team {teamId} not found.");
                    if (team.LeagueId != request.LeagueId)
                        throw new ForbiddenAccessException("Team does not belong to this league.");
                    var assignment = new TeamDivisionSeason(team, divisionSeason);
                    await _teamDivisionSeasonRepository.AddAsync(assignment, cancellationToken);
                }
            }

            // Unlocked divisions that disappeared from the request (or emptied): clear teams.
            foreach (var ds in existingDivisionSeasons)
            {
                if (lockedDivisionIds.Contains(ds.DivisionId))
                    continue;

                var requested = divisions.FirstOrDefault(d => d.DivisionId == ds.DivisionId);
                if (requested != null && requested.TeamIds.Count > 0)
                    continue;

                // Already cleared above when requested with empty/non-empty rewrite;
                // if omitted or empty and wasn't rewritten in the loop with teams, clear now.
                if (requested == null || requested.TeamIds.Count == 0)
                {
                    // If requested with empty list, Remove already ran in the loop when divisionSeason existed.
                    // If omitted from request, remove here.
                    if (requested == null)
                    {
                        await _teamDivisionSeasonRepository.RemoveByDivisionSeasonIdAsync(ds.Id, cancellationToken);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
