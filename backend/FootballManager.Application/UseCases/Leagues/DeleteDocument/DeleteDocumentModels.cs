using System;
using System.Threading;
using System.Threading.Tasks;

namespace FootballManager.Application.UseCases.Leagues.DeleteDocument
{
    public interface IDeleteDocumentUseCase
    {
        Task<DeleteDocumentResponse> ExecuteAsync(DeleteDocumentRequest request, CancellationToken cancellationToken = default);
    }

    public class DeleteDocumentRequest
    {
        public Guid LeagueId { get; set; }
        public Guid DocumentId { get; set; }
        public Guid UserId { get; set; }
    }

    public class DeleteDocumentResponse
    {
        public string? RelativePath { get; }

        public DeleteDocumentResponse(string? relativePath)
        {
            RelativePath = relativePath;
        }
    }
}
