using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateTeam
{
    public class UpdateTeamUseCase : IUpdateTeamUseCase
    {
        private readonly ITeamRepository _teamRepository;
        private readonly IClubRepository _clubRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateTeamUseCase(
            ITeamRepository teamRepository,
            IClubRepository clubRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateTeamRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Team name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var team = await _teamRepository.GetByIdAsync(request.TeamId, cancellationToken);
            if (team == null)
                throw new KeyNotFoundException($"Team {request.TeamId} not found.");
            if (team.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Team does not belong to this league.");

            if (request.ClubId.HasValue)
            {
                var club = await _clubRepository.GetByIdAsync(request.ClubId.Value, cancellationToken);
                if (club == null || club.LeagueId != request.LeagueId)
                    throw new BusinessException("Club not found in this league.");
            }

            team.UpdateName(request.Name, request.ShortName, request.Suffix);
            team.AssignClub(request.ClubId);
            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var slug = SlugGenerator.Generate(request.Slug);
                var existing = await _teamRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
                var slugs = existing.Where(t => t.Id != request.TeamId).Select(t => t.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var finalSlug = slug;
                var counter = 1;
                while (slugs.Contains(finalSlug))
                {
                    finalSlug = $"{slug}-{counter}";
                    counter++;
                }
                team.SetSlug(finalSlug);
            }
            team.UpdateDetails(request.PrimaryColor, request.SecondaryColor, request.LogoUrl, request.Email, request.PhotoUrl);
            team.SetDelegateInfo(request.DelegateName, request.DelegateContact);
            team.SetFoundedYear(request.FoundedYear);
            _teamRepository.Update(team);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
