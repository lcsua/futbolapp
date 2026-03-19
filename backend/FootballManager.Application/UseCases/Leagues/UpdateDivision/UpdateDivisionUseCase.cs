using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateDivision
{
    public class UpdateDivisionUseCase : IUpdateDivisionUseCase
    {
        private readonly IDivisionRepository _divisionRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDivisionUseCase(
            IDivisionRepository divisionRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _divisionRepository = divisionRepository ?? throw new ArgumentNullException(nameof(divisionRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateDivisionRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Division name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var division = await _divisionRepository.GetByIdAsync(request.DivisionId, cancellationToken);
            if (division == null)
                throw new KeyNotFoundException($"Division {request.DivisionId} not found.");
            if (division.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Division does not belong to this league.");

            var slug = !string.IsNullOrWhiteSpace(request.Slug)
                ? SlugGenerator.Generate(request.Slug)
                : SlugGenerator.Generate(request.Name);
            var existing = await _divisionRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var slugs = existing.Where(d => d.Id != request.DivisionId).Select(d => d.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var finalSlug = slug;
            var counter = 1;
            while (slugs.Contains(finalSlug))
            {
                finalSlug = $"{slug}-{counter}";
                counter++;
            }

            division.UpdateDetails(request.Name, finalSlug, request.Description);
            _divisionRepository.Update(division);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
