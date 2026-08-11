using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.UpdateDocument
{
    public class UpdateDocumentUseCase : IUpdateDocumentUseCase
    {
        private readonly ILeagueDocumentRepository _documentRepository;
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateDocumentUseCase(
            ILeagueDocumentRepository documentRepository,
            ILeagueDocumentCategoryRepository categoryRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task ExecuteAsync(UpdateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Document title is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
            if (document == null)
                throw new KeyNotFoundException($"Document {request.DocumentId} not found.");
            if (document.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Document does not belong to this league.");

            var category = await _categoryRepository.GetByIdAsync(document.CategoryId, cancellationToken);
            if (category != null && category.RequiresDocumentDate && !request.DocumentDate.HasValue)
                throw new BusinessException("Document date is required for this category.");

            try
            {
                document.Update(
                    request.Title,
                    request.Description,
                    request.DocumentDate,
                    request.IsPublished,
                    request.FileUrl,
                    request.RelativePath,
                    request.OriginalFileName,
                    request.ContentType,
                    request.FileSizeBytes);

                if (request.SortOrder.HasValue)
                    document.SetSortOrder(request.SortOrder.Value);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            _documentRepository.Update(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
