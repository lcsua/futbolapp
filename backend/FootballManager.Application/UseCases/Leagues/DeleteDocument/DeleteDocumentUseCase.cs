using System;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.DeleteDocument
{
    public class DeleteDocumentUseCase : IDeleteDocumentUseCase
    {
        private readonly ILeagueDocumentRepository _documentRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteDocumentUseCase(
            ILeagueDocumentRepository documentRepository,
            IUserLeagueRepository userLeagueRepository,
            IUnitOfWork unitOfWork)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<DeleteDocumentResponse> ExecuteAsync(DeleteDocumentRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
            if (document == null)
                throw new KeyNotFoundException($"Document {request.DocumentId} not found.");
            if (document.LeagueId != request.LeagueId)
                throw new ForbiddenAccessException("Document does not belong to this league.");

            var relativePath = document.RelativePath;
            _documentRepository.Remove(document);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteDocumentResponse(relativePath);
        }
    }
}
