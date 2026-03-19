using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateTeam
{
    public class CreateTeamUseCase : ICreateTeamUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeamUseCase(
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

        public async Task<CreateTeamResponse> ExecuteAsync(CreateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Team name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var baseSlug = SlugGenerator.Generate(request.Slug ?? request.Name);
            var slug = await EnsureUniqueTeamSlugAsync(request.LeagueId, baseSlug, cancellationToken);

            var team = new Team(league, request.Name, slug, request.ShortName, request.Email);
            await _teamRepository.AddAsync(team, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateTeamResponse(team.Id);
        }

        private async Task<string> EnsureUniqueTeamSlugAsync(Guid leagueId, string baseSlug, CancellationToken cancellationToken)
        {
            var existing = await _teamRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
            var slugs = existing.Select(t => t.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var slug = baseSlug;
            var counter = 1;
            while (slugs.Contains(slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            return slug;
        }
    }
}
