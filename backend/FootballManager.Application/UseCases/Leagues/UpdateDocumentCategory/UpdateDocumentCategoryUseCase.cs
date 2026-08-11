using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateDocumentCategory
{
    public class UpdateDocumentCategoryUseCase : IUpdateDocumentCategoryUseCase
    {
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDocumentCategoryUseCase(
            ILeagueDocumentCategoryRepository categoryRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateDocumentCategoryRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Category name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Document category {request.CategoryId} not found.");
            if (category.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Category does not belong to this league.");

            var baseSlug = SlugGenerator.Generate(!string.IsNullOrWhiteSpace(request.Slug) ? request.Slug : request.Name);
            if (string.IsNullOrWhiteSpace(baseSlug))
                throw new ArgumentException("Could not generate a valid slug from the category name.");

            var slug = await EnsureUniqueSlugAsync(request.LeagueId, baseSlug, request.CategoryId, cancellationToken);

            try
            {
                category.Update(request.Name, slug, request.SortOrder, request.RequiresDocumentDate, request.IsActive);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            _categoryRepository.Update(category);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        private async Task<string> EnsureUniqueSlugAsync(Guid leagueId, string baseSlug, Guid excludeId, CancellationToken cancellationToken)
        {
            var existing = await _categoryRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
            var slugs = existing.Where(c => c.Id != excludeId).Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
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
