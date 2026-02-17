using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.BulkCreateTeams
{
    public class BulkCreateTeamsUseCase : IBulkCreateTeamsUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public BulkCreateTeamsUseCase(
            ILeagueRepository leagueRepository,
            ITeamRepository teamRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        private static string GenerateShortName(string name)
        {
            return name.Length <= 20 ? name : name.Substring(0, 20);
        }

        public async Task<BulkCreateTeamsResponse> ExecuteAsync(BulkCreateTeamsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var existingTeams = await _teamRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var existingNames = new HashSet<string>(existingTeams.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);

            var namesToCreate = (request.Names ?? new List<string>())
                .Select(n => n?.Trim() ?? string.Empty)
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(n => !existingNames.Contains(n))
                .ToList();

            var createdIds = new List<Guid>();

            foreach (var name in namesToCreate)
            {
                var shortName = GenerateShortName(name);
                var team = new Team(league, name, shortName, email: null);
                team.UpdateDetails("#FFFFFF", "#FFFFFF", "/images/default-team.png", null, "/images/default-team.png");
                team.SetDelegateInfo("--", "--");

                await _teamRepository.AddAsync(team, cancellationToken);
                createdIds.Add(team.Id);
                existingNames.Add(name);
            }

            if (createdIds.Count > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new BulkCreateTeamsResponse(createdIds);
        }
    }
}
