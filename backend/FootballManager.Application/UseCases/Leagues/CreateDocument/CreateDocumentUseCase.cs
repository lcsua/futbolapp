using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;
using FootballManager.Domain.Entities;

namespace FootballManager.Application.UseCases.Leagues.CreateDocument
{
    public class CreateDocumentUseCase : ICreateDocumentUseCase
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ILeagueDocumentCategoryRepository _categoryRepository;
        private readonly ILeagueDocumentRepository _documentRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateDocumentUseCase(
            ILeagueRepository leagueRepository,
            ILeagueDocumentCategoryRepository categoryRepository,
            ILeagueDocumentRepository documentRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _leagueRepository = leagueRepository ?? throw new ArgumentNullException(nameof(leagueRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<CreateDocumentResponse> ExecuteAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Document title is required.");
            if (string.IsNullOrWhiteSpace(request.FileUrl))
                throw new ArgumentException("File URL is required.");
            if (string.IsNullOrWhiteSpace(request.RelativePath))
                throw new ArgumentException("Relative path is required.");
            if (string.IsNullOrWhiteSpace(request.ContentType))
                throw new ArgumentException("Content type is required.");
            if (string.IsNullOrWhiteSpace(request.OriginalFileName))
                throw new ArgumentException("Original file name is required.");

            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var league = await _leagueRepository.GetByIdAsync(request.LeagueId, cancellationToken);
            if (league == null)
                throw new KeyNotFoundException($"League {request.LeagueId} not found.");

            var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
            if (category == null)
                throw new KeyNotFoundException($"Document category {request.CategoryId} not found.");
            if (category.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Category does not belong to this league.");
            if (!category.IsActive)
                throw new BusinessException("Cannot add documents to an inactive category.");

            if (category.RequiresDocumentDate && !request.DocumentDate.HasValue)
                throw new BusinessException("Document date is required for this category.");

            var sortOrder = request.SortOrder ?? await NextSortOrderAsync(request.LeagueId, request.CategoryId, cancellationToken);

            LeagueDocument document;
            try
            {
                document = new LeagueDocument(
                    league,
                    category,
                    request.Title,
                    request.FileUrl,
                    request.RelativePath,
                    request.OriginalFileName,
                    request.ContentType,
                    request.FileSizeBytes,
                    request.Description,
                    request.DocumentDate,
                    sortOrder,
                    request.IsPublished);
            }
            catch (ArgumentException ex)
            {
                throw new BusinessException(ex.Message);
            }

            await _documentRepository.AddAsync(document, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateDocumentResponse(document.Id);
        }

        private async Task<int> NextSortOrderAsync(Guid leagueId, Guid categoryId, CancellationToken cancellationToken)
        {
            var existing = await _documentRepository.GetByLeagueIdAsync(leagueId, categoryId, cancellationToken);
            if (existing.Count == 0)
                return 0;
            return existing.Max(d => d.SortOrder) + 1;
        }
    }
}
