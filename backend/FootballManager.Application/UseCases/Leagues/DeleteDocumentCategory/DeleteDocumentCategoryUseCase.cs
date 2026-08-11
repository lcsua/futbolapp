using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteDocumentCategory
{
    public class DeleteDocumentCategoryUseCase : IDeleteDocumentCategoryUseCase
    {
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteDocumentCategoryUseCase(
            ILeagueDocumentCategoryRepository categoryRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(DeleteDocumentCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Document category {request.CategoryId} not found.");
            if (category.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Category does not belong to this league.");

            var docCount = await _categoryRepository.CountDocumentsAsync(category.Id, cancellationToken);
            if (docCount == 0)
            {
                _categoryRepository.Remove(category);
            }
            else
            {
                category.Deactivate();
                _categoryRepository.Update(category);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
