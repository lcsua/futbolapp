using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.SeedLeagueDocumentDefaults
{
    public class SeedLeagueDocumentDefaultsUseCase : ISeedLeagueDocumentDefaultsUseCase
    {
        public static readonly (string Name, string Slug, int SortOrder, bool RequiresDocumentDate)[] DefaultCategories =
        {
            ("Información útil", "informacion-util", 1, false),
            ("Resoluciones", "resoluciones", 2, true),
        };

        private readonly ILeagueRepository _leagueRepository;
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SeedLeagueDocumentDefaultsUseCase(
            ILeagueRepository leagueRepository,
            ILeagueDocumentCategoryRepository categoryRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<SeedLeagueDocumentDefaultsResponse> ExecuteAsync(
            SeedLeagueDocumentDefaultsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.RequireMembership)
            {
                if (!request.UserId.HasValue || request.UserId.Value == Guid.Empty)
                    throw new ForbiddenAccessException("User is required.");

                var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId.Value, request.LeagueId, cancellationToken);
                if (!hasAccess)
                    throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");
            }

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var created = 0;
            foreach (var def in DefaultCategories)
            {
                var exists = await _categoryRepository.ExistsByLeagueAndSlugAsync(request.LeagueId, def.Slug, cancellationToken);
                if (exists)
                    continue;

                var category = new LeagueDocumentCategory(
                    league,
                    def.Name,
                    def.Slug,
                    def.SortOrder,
                    def.RequiresDocumentDate);
                await _categoryRepository.AddAsync(category, cancellationToken);
                created++;
            }

            if (created > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new SeedLeagueDocumentDefaultsResponse(created);
        }
    }
}
