using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
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
        private readonly IUnitOfWork _unitOfWork;

        public SaveSeasonSetupUseCase(
            IUserLeagueRepository userLeagueRepository,
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            ITeamRepository teamRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            ITeamDivisionSeasonRepository teamDivisionSeasonRepository,
            IUnitOfWork unitOfWork)
        {
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
            _teamDivisionSeasonRepository = teamDivisionSeasonRepository ?? throw new ArgumentNullException(nameof(teamDivisionSeasonRepository));
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

            await _teamDivisionSeasonRepository.RemoveBySeasonIdAsync(request.SeasonId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var divDto in divisions)
            {
                if (divDto.TeamIds.Count == 0)
                    continue;

                var division = await _divisionRepository.GetByIdAsync(divDto.DivisionId, cancellationToken);
                if (division == null)
                    throw new KeyNotFoundException($"Division {divDto.DivisionId} not found.");
                if (division.LeagueId != request.LeagueId)
                    throw new ForbiddenAccessException("Division does not belong to this league.");

                var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionAsync(request.SeasonId, divDto.DivisionId, cancellationToken);
                if (divisionSeason == null)
                {
                    divisionSeason = new DivisionSeason(season, division);
                    await _divisionSeasonRepository.AddAsync(divisionSeason, cancellationToken);
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

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
