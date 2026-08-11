using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Helpers;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateDocumentCategory
{
    public class CreateDocumentCategoryUseCase : ICreateDocumentCategoryUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDocumentCategoryUseCase(
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

        public async Task<CreateDocumentCategoryResponse> ExecuteAsync(
            CreateDocumentCategoryRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("Category name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var baseSlug = SlugGenerator.Generate(request.Name);
            if (string.IsNullOrWhiteSpace(baseSlug))
                throw new ArgumentException("Could not generate a valid slug from the category name.");

            var slug = await EnsureUniqueSlugAsync(request.LeagueId, baseSlug, cancellationToken);
            var sortOrder = request.SortOrder ?? await NextSortOrderAsync(request.LeagueId, cancellationToken);

            LeagueDocumentCategory category;
            try
            {
                category = new LeagueDocumentCategory(league, request.Name, slug, sortOrder, request.RequiresDocumentDate);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateDocumentCategoryResponse(category.Id, category.Slug);
        }

        private async Task<string> EnsureUniqueSlugAsync(Guid leagueId, string baseSlug, CancellationToken cancellationToken)
        {
            var existing = await _categoryRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
            var slugs = existing.Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var slug = baseSlug;
            var counter = 1;
            while (slugs.Contains(slug))
            {
                slug = $"{baseSlug}-{counter}";
                counter++;
            }
            return slug;
        }

        private async Task<int> NextSortOrderAsync(Guid leagueId, CancellationToken cancellationToken)
        {
            var existing = await _categoryRepository.GetByLeagueIdAsync(leagueId, cancellationToken);
            if (existing.Count == 0)
                return 1;
            return existing.Max(c => c.SortOrder) + 1;
        }
    }
}
