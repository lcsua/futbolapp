using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FootballManager.Application.Exceptions;
using FootballManager.Application.Interfaces.Repositories;

namespace FootballManager.Application.UseCases.Leagues.GetDocuments
{
    public class GetDocumentsUseCase : IGetDocumentsUseCase
    {
        private readonly ILeagueDocumentRepository _documentRepository;
        private readonly IUserLeagueRepository _userLeagueRepository;

        public GetDocumentsUseCase(
            ILeagueDocumentRepository documentRepository,
            IUserLeagueRepository userLeagueRepository)
        {
            _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
            _userLeagueRepository = userLeagueRepository ?? throw new ArgumentNullException(nameof(userLeagueRepository));
        }

        public async Task<GetDocumentsResponse> ExecuteAsync(GetDocumentsRequest request, CancellationToken cancellationToken = default)
        {
            var hasAccess = await _userLeagueRepository.IsUserInLeagueAsync(request.UserId, request.LeagueId, cancellationToken);
            if (!hasAccess)
                throw new ForbiddenAccessException($"User {request.UserId} does not have access to league {request.LeagueId}.");

            var documents = await _documentRepository.GetByLeagueIdAsync(request.LeagueId, request.CategoryId, cancellationToken);
            var dtos = documents.Select(d => new DocumentDto(
                d.Id,
                d.CategoryId,
                d.Title,
                d.Description,
                d.FileUrl,
                d.RelativePath,
                d.OriginalFileName,
                d.ContentType,
                d.FileSizeBytes,
                d.DocumentDate,
                d.SortOrder,
                d.IsPublished)).ToList();

            return new GetDocumentsResponse(dtos);
        }
    }
}
