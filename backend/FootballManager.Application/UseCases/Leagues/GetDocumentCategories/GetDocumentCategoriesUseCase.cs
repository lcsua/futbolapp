using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetDocumentCategories
{
    public class GetDocumentCategoriesUseCase : IGetDocumentCategoriesUseCase
    {
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetDocumentCategoriesUseCase(
            ILeagueDocumentCategoryRepository categoryRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetDocumentCategoriesResponse> ExecuteAsync(
            GetDocumentCategoriesRequest request,
            CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var categories = await _categoryRepository.GetByLeagueIdAsync(request.LeagueId, cancellationToken);
            var dtos = categories.Select(c => new DocumentCategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.SortOrder,
                c.RequiresDocumentDate,
                c.IsActive)).ToList();

            return new GetDocumentCategoriesResponse(dtos);
        }
    }
}
