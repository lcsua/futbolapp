using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Dtos;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.GetSeasonSetup
{
    public class GetSeasonSetupUseCase : IGetSeasonSetupUseCase
    {
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IDivisionRepository _divisionRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;

        public GetSeasonSetupUseCase(
            IUserLeagueRepository userLeagueRepository,
            ISeasonRepository seasonRepository,
            IDivisionRepository divisionRepository,
            ITeamRepository teamRepository,
            IDivisionSeasonRepository divisionSeasonRepository)
        {
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
        }

        public async Task<GetSeasonSetupResponse> ExecuteAsync(GetSeasonSetupRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User does not have access to league {request.LeagueId}.");

            var season = await _seasonRepository.GetByIdAsync(request.SeasonId, cancellationToken);
            if (season == null)
                throw new KeyNotFoundException($"Season {request.SeasonId} not found.");
            if (season.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Season does not belong to this league.");

            var divisions = await _divisionRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var teams = await _teamRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var divisionSeasons = await _divisionSeasonRepository.GetBySeasonIdAsync(request.SeasonId, cancellationToken);

            var assignedTeamIds = new HashSet<Guid>();
            var divisionMap = new Dictionary<Guid, List<TeamDto>>();
            foreach (var div in divisions)
                divisionMap[div.Id] = new List<TeamDto>();

            foreach (var ds in divisionSeasons)
            {
                var teamDtos = ds.TeamAssignments
                    .Select(ta => ToTeamDto(ta.Team))
                    .ToList();
                foreach (var t in teamDtos)
                    assignedTeamIds.Add(t.Id);
                divisionMap[ds.DivisionId] = teamDtos;
            }

            var unassignedTeams = teams
                .Where(t => !assignedTeamIds.Contains(t.Id))
                .Select(ToTeamDto)
                .OrderBy(t => t.Name)
                .ToList();

            var divisionDtos = divisions
                .OrderBy(d => d.Name)
                .Select(d => new SeasonSetupDivisionDto(d.Id, d.Name, divisionMap[d.Id]))
                .ToList();

            return new GetSeasonSetupResponse(unassignedTeams, divisionDtos);
        }

        private static TeamDto ToTeamDto(Team t)
        {
            return new TeamDto(t.Id, t.Name, t.ShortName, t.LogoUrl, t.Email, t.FoundedYear, t.DelegateName, t.DelegateContact, t.PhotoUrl);
        }
    }
}
