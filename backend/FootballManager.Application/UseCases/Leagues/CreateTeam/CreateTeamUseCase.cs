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
        private readonly IClubRepository _clubRepository;
        private readonly IDivisionSeasonRepository _divisionSeasonRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTeamUseCase(
            ILeagueRepository leagueRepository,
            ITeamRepository teamRepository,
            IClubRepository clubRepository,
            IDivisionSeasonRepository divisionSeasonRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _clubRepository = clubRepository ?? throw new ArgumentNullException(nameof(clubRepository));
            _divisionSeasonRepository = divisionSeasonRepository ?? throw new ArgumentNullException(nameof(divisionSeasonRepository));
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

            if (request.ClubId.HasValue)
            {
                var club = await _clubRepository.GetByIdAsync(request.ClubId.Value, cancellationToken);
                if (club == null || club.LeagueId != request.LeagueId)
                    throw new BusinessException("Club not found in this league.");
            }

            var suffixToUse = await ResolveSuffixAsync(request, cancellationToken);
            var team = new Team(league, request.Name, slug, request.ShortName, request.Email, request.ClubId, suffixToUse);
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

        private async Task<string?> ResolveSuffixAsync(CreateTeamRequest request, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(request.Suffix))
                return request.Suffix;

            if (!request.SeasonId.HasValue || !request.DivisionId.HasValue)
                return null;

            var divisionSeason = await _divisionSeasonRepository.GetBySeasonAndDivisionWithTeamsAsync(
                request.SeasonId.Value,
                request.DivisionId.Value,
                cancellationToken);
            if (divisionSeason == null)
                return null;
            if (divisionSeason.Division.LeagueId != request.LeagueId || divisionSeason.Season.LeagueId != request.LeagueId)
                return null;

            var sameNameTeams = divisionSeason.TeamAssignments
                .Select(ta => ta.Team)
                .Where(t => string.Equals(t.Name, request.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sameNameTeams.Count == 0)
                return null;

            var usedSuffixes = sameNameTeams
                .Select(t => t.Suffix?.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.ToUpperInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var index = 0;
            while (true)
            {
                var candidate = ToAlphabeticSuffix(index);
                if (!usedSuffixes.Contains(candidate))
                    return candidate;

                index++;
            }
        }

        private static string ToAlphabeticSuffix(int index)
        {
            index++;
            var result = string.Empty;
            while (index > 0)
            {
                index--;
                result = (char)('A' + (index % 26)) + result;
                index /= 26;
            }

            return result;
        }
    }
}
